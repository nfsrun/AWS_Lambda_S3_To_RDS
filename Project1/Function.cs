using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;

using Npgsql;

using Newtonsoft.Json.Linq;
using Amazon.S3.Model;
using System.Text;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Project1
{
    public class Function
    {
        // internal class to organize data grabbing by an invocation
        internal class EntryRow
        {
            internal int id { get; set; }
            internal int? age { get; set; }
            internal string gender = null;
            internal string maritalStatus = null;
            internal double? bmi { get; set; }
            internal string smoker = null;
            internal string alcoholConsumption = null;
            internal int? totalCholesterol { get; set; }
            internal int? ldlCholesterol { get; set; }
            internal int? hdlCholesterol { get; set; }
            internal int? triglycerides { get; set; }
            internal int? plasmaCeramides { get; set; }
            internal int? natriureticPeptide { get; set; }
            internal string hasVascularDisease = null;

            public string ToString()
            {
                return id + " " + age + " " + gender + " " + maritalStatus + " " + bmi + " " + smoker + " " + alcoholConsumption + " " + totalCholesterol + " " + ldlCholesterol + " " + hdlCholesterol + " " + triglycerides + " " + plasmaCeramides + " " + natriureticPeptide + " " + hasVascularDisease;
            }
        }

        IAmazonS3 S3Client { get; set; }

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {
            S3Client = new AmazonS3Client();
        }

        /// <summary>
        /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
        /// </summary>
        /// <param name="s3Client"></param>
        public Function(IAmazonS3 s3Client)
        {
            this.S3Client = s3Client;
        }
        
        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
        /// to respond to S3 notifications.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            var s3Event = evnt.Records?[0].S3;
            if (s3Event != null)
            {
                string bucketName = s3Event.Bucket.Name;
                string objectKey = s3Event.Object.Key;
                string extension = objectKey.Split(".").Last().ToLower().TrimEnd();
                context.Logger.LogLine(evnt.Records[0].EventName + " for " + extension + " file named " + objectKey + " in " + bucketName);

                try
                {
                    Stream stream = await S3Client.GetObjectStreamAsync(bucketName, objectKey, null);
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        NpgsqlConnection connection = Function.OpenConnection(context);
                        string output = reader.ReadToEnd();
                        context.Logger.LogLine(output);

                        //if event detected an item is put/marked removed (not activated, testing) and the xml or json is valid, then add or remove
                        EntryRow content = null;
                        if (evnt.Records[0].EventName == EventType.ObjectCreatedPut && (content = CheckValidXmlorJson(output, extension, false, context)) != null)
                            AddObject(content, connection, context);
                        else if (evnt.Records[0].EventName == EventType.ObjectRemovedDeleteMarkerCreated && (content = CheckValidXmlorJson(output, extension, true, context)) != null)
                            DeleteObject(content, connection, context);
                        reader.Close();
                    }
                    //kept original requests/return here. 
                    GetObjectMetadataResponse res = await this.S3Client.GetObjectMetadataAsync(bucketName, objectKey);
                    return res.ResponseMetadata.Metadata.ToString();
                }
                catch (Exception e)
                {
                    context.Logger.LogLine($"Error getting object {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
                    context.Logger.LogLine(e.Message);
                    context.Logger.LogLine(e.StackTrace);
                    context.Logger.LogLine(e.HelpLink);
                    throw;
                }
            }
            return null;
        }

        //Takes the file string input for further processing with the internal 
        internal EntryRow CheckValidXmlorJson(string input, string extension, bool del, ILambdaContext context)
        {
            EntryRow output = null;
            if (extension.Equals("xml"))
            {
                context.Logger.LogLine("Attempting to parse XML contents... ");
                try
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(input);
                    XmlNode parent = xmlDoc.FirstChild;

                    //Process only if the XML starts with patient and has an id on the first level
                    if (parent.Name.Equals("patient") && parent["id"] != null)
                    {
                        output = new EntryRow();
                        output.id = int.Parse(parent["id"].InnerText);
                        //if delimiter is true for deleting, just get out of the method. 
                        if (del)
                            return output;
                        
                        if (parent["age"] != null && int.TryParse(parent["age"].InnerText, out int i))
                            output.age = int.Parse(parent["age"].InnerText);
                        if (parent["gender"] != null)
                            output.gender = parent["gender"].InnerText;
                        if (parent["maritalStatus"] != null)
                            output.maritalStatus = parent["maritalStatus"].InnerText;
                        if (parent["bmi"] != null && double.TryParse(parent["bmi"].InnerText, out double temp))
                            output.bmi = double.Parse(parent["bmi"].InnerText);
                        if (parent["smoker"] != null)
                            output.smoker = parent["smoker"].InnerText;
                        if (parent["alcoholConsumption"] != null)
                            output.alcoholConsumption = parent["alcoholConsumption"].InnerText;
                        if (parent["hasVascularDisease"] != null)
                            output.hasVascularDisease = parent["hasVascularDisease"].InnerText;

                        XmlNode tests = parent["tests"];
                        if (tests != null)
                        {
                            //If tests node on xml is valid go through all of them and define them if valid; 
                            foreach (XmlNode test in tests.ChildNodes)
                            {
                                switch (test.Attributes["name"].Value)
                                {
                                    case "total-cholesterol":
                                        output.totalCholesterol = int.Parse(test.InnerText);
                                        break;
                                    case "LDL-cholesterol":
                                        output.ldlCholesterol = int.Parse(test.InnerText);
                                        break;
                                    case "HDL-cholesterol":
                                        output.hdlCholesterol = int.Parse(test.InnerText);
                                        break;
                                    case "triglycerides":
                                        output.triglycerides = int.Parse(test.InnerText);
                                        break;
                                    case "plasmaCeramides":
                                        output.plasmaCeramides = int.Parse(test.InnerText);
                                        break;
                                    case "natriureticPeptide":
                                        output.natriureticPeptide = int.Parse(test.InnerText);
                                        break;
                                    default:
                                        context.Logger.LogLine(test.Name + " does not exist for the health template. ");
                                        break;
                                }

                            }
                        }
                        context.Logger.LogLine("XML contents are prepped. ");
                    }
                    else
                        context.Logger.LogLine("XML contents are invalid... Lambda function will stop. ");
                }
                catch (XmlException e)
                {
                    context.Logger.LogLine("Contents did not create valid XML... Lambda function will stop. ");
                }
            }
            else if (extension.Equals("json"))
            {
                try
                {
                    context.Logger.LogLine("Attempting to parse JSON contents... ");
                    JObject jsonOut = JObject.Parse(input);
                    //if json has id then continue
                    if (jsonOut["id"] != null)
                    {
                        output = new EntryRow();
                        output.id = int.Parse((string)jsonOut["id"]);
                        //if delimiter is true for deleting, just get out of the method. 
                        if (del)
                            return output;
                        if ((string)jsonOut["age"] != null && int.TryParse((string)jsonOut["age"], out int i))
                            output.age = int.Parse((string)jsonOut["age"]);
                        if (jsonOut["gender"] != null)
                            output.gender = (string)jsonOut["gender"];
                        if (jsonOut["maritalStatus"] != null)
                            output.maritalStatus = (string)jsonOut["maritalStatus"];
                        if ((string)jsonOut["bmi"] != null && double.TryParse((string)jsonOut["bmi"], out double j))
                            output.bmi = double.Parse((string)jsonOut["bmi"]);
                        if (jsonOut["smoker"] != null)
                            output.smoker = (string)jsonOut["smoker"];
                        if (jsonOut["alcoholConsumtion"] != null)
                            output.alcoholConsumption = (string)jsonOut["alcoholConsumtion"];
                        if (jsonOut["hasVascularDisease"] != null)
                            output.hasVascularDisease = (string)jsonOut["hasVascularDisease"];
                        
                        //if jsonOut tests exists go through each json sub-element and add into the EntryRow data structure as applicable
                        if (jsonOut["tests"] != null)
                        {
                            JArray tests = jsonOut["tests"].Value<JArray>();
                            if (tests != null)
                            {
                                foreach (JToken test in tests)
                                {
                                    switch ((string)test["name"])
                                    {
                                        case "total-cholesterol":
                                            output.totalCholesterol = int.Parse((string)test["value"]);
                                            break;
                                        case "LDL-cholesterol":
                                            output.ldlCholesterol = int.Parse((string)test["value"]);
                                            break;
                                        case "HDL-cholesterol":
                                            output.hdlCholesterol = int.Parse((string)test["value"]);
                                            break;
                                        case "triglycerides":
                                            output.triglycerides = int.Parse((string)test["value"]);
                                            break;
                                        case "plasmaCeramides":
                                            output.plasmaCeramides = int.Parse((string)test["value"]);
                                            break;
                                        case "natriureticPeptide":
                                            output.natriureticPeptide = int.Parse((string)test["value"]);
                                            break;
                                        default:
                                            Console.WriteLine((string)test["name"] + " does not exist for the health template. ");
                                            break;
                                    }
                                }
                            }
                        }
                        context.Logger.LogLine("JSON contents are prepped. ");
                    }
                    else
                        context.Logger.LogLine("JSON contents are invalid... Lambda function will stop. ");
                }
                catch (Newtonsoft.Json.JsonReaderException e)
                {
                    context.Logger.LogLine("Contents did not create valid JSON... Lambda function will stop. ");
                }
            }
            else
                context.Logger.LogLine(" extension is an invalid extension. Lambda function will stop. ");
            return output;
        }

        //DeleteObject method just in case if it can be used to remove entry when object is removed. 
        private void DeleteObject(EntryRow entry, NpgsqlConnection conn, ILambdaContext context)
        {
            NpgsqlCommand command = new NpgsqlCommand("DELETE FROM health.\"healthTable\" WHERE id == " + entry.id + ";", conn);
            context.Logger.LogLine(command.Statements[0].SQL);
            command.ExecuteNonQuery();
            conn.Close();
            context.Logger.LogLine("DELETE command sent. Connection is now closed. ");
        }

        //AddObject method will do INSERT code
        private void AddObject(EntryRow entry, NpgsqlConnection conn, ILambdaContext context)
        {

            StringBuilder sb = new StringBuilder();
            sb.Append(entry.id + ",");
            if (entry.age != null)
                sb.Append(entry.age + ",");
            else
                sb.Append("NULL,");
            if (entry.gender != null)
                sb.Append("'" + entry.gender + "',");
            else
                sb.Append("NULL,");
            if (entry.maritalStatus != null)
                sb.Append("'" + entry.maritalStatus + "',");
            else
                sb.Append("NULL,");
            if (entry.bmi != null)
                sb.Append(entry.bmi + ",");
            else
                sb.Append("NULL,");
            if (entry.smoker != null)
                sb.Append("'" + entry.smoker + "',");
            else
                sb.Append("NULL,");
            if (entry.alcoholConsumption != null)
                sb.Append("'" + entry.alcoholConsumption + "',");
            else
                sb.Append("NULL,");
            if (entry.totalCholesterol != null)
                sb.Append(entry.totalCholesterol + ",");
            else
                sb.Append("NULL,");
            if (entry.ldlCholesterol != null)
                sb.Append(entry.ldlCholesterol + ",");
            else
                sb.Append("NULL,");
            if (entry.hdlCholesterol != null)
                sb.Append(entry.hdlCholesterol + ",");
            else
                sb.Append("NULL,");
            if (entry.triglycerides != null)
                sb.Append(entry.triglycerides + ",");
            else
                sb.Append("NULL,");
            if (entry.plasmaCeramides != null)
                sb.Append(entry.plasmaCeramides + ",");
            else
                sb.Append("NULL,");
            if (entry.natriureticPeptide != null)
                sb.Append(entry.natriureticPeptide + ",");
            else
                sb.Append("NULL,");
            if (entry.hasVascularDisease != null)
                sb.Append("'" + entry.hasVascularDisease + "'");
            else
                sb.Append("NULL");
            string input = "INSERT INTO health.\"healthTable\" (id, age, gender, \"maritalStatus\", bmi, smoker, \"alcoholConsumption\", \"totalCholesterol\", \"LDLCholesterol\", \"HDLCholesterol\", triglycerides, \"plasmaCeramides\", \"natriureticPeptide\", \"hasVascularDisease\") VALUES (" + sb.ToString() + ");";

            NpgsqlCommand command = new NpgsqlCommand(input, conn);
            command.ExecuteNonQuery();
            conn.Close();
            context.Logger.LogLine("INSERT command sent. Connection is now closed. ");
        }

        //Open Connection method to get connection to the Postgres DB
        private static NpgsqlConnection OpenConnection(ILambdaContext context)
        {
            //Must figure out the connection  string
            string connString = @"Server=healthdbinstance.cpwhiglhncbi.us-east-1.rds.amazonaws.com;" +
                "Port=5432;" +
                "Database=healthdb;" +
                "User Id=root;" +
                "Password=password;" +
                "Timeout=25";
            try
            {
                NpgsqlConnection conn = new NpgsqlConnection(connString);
                conn.Open();

                if (conn.State == System.Data.ConnectionState.Open)
                {
                    context.Logger.LogLine("Connection successful; connection object returned. ");
                    return conn;
                }
                else
                    context.Logger.LogLine("Connection to PostgreSQL database testdb failed to open. ");
            }
            catch (NpgsqlException ex)
            {
                context.Logger.LogLine("Error openning connection to testdb: " + ex.Message);
            }
            catch (Exception ex)
            {
                context.Logger.LogLine("Error openning connection to testdb: " + ex.Message);
            }
            return null;
        }
    }
}
