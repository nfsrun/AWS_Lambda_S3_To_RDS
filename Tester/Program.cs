using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Xml;
using System.Text;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {

            string input = File.ReadAllText(@"C:\Users\user\OneDrive - Bellevue College\Project1Cloud\patient9.json");
            EntryRow entry = CheckValidXmlorJson(input, "json", false);
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
            string input2 = "INSERT INTO health.\"healthTable\" (id, age, gender, \"maritalStatus\", bmi, smoker, \"alcoholConsumption\", \"totalCholesterol\", \"LDLCholesterol\", \"HDLCholesterol\", triglycerides, \"plasmaCeramides\", \"natriureticPeptide\", \"hasVascularDisease\") VALUES (" + sb.ToString() + ");";

            Console.WriteLine(input2);
            Console.Read();
        }
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
        internal static EntryRow CheckValidXmlorJson(string input, string extension, bool del)
        {
            EntryRow output = null;
            if (extension.Equals("xml"))
            {
                Console.WriteLine("Attempting to parse XML contents... ");
                try
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(input);
                    XmlNode parent = xmlDoc.FirstChild;

                    if (parent.Name.Equals("patient") && parent["id"] != null)
                    {
                        output = new EntryRow();
                        output.id = int.Parse(parent["id"].InnerText);
                        if (del)
                            return output;
                        if(parent["age"] != null)
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
                                        Console.WriteLine(test.Name + " does not exist for the health template. ");
                                        break;
                                }

                            }
                        }
                        Console.WriteLine("XML contents are prepped. ");
                    }
                    else
                        Console.WriteLine("XML contents are invalid... Lambda function will stop. ");
                }
                catch (XmlException e)
                {
                    Console.WriteLine("Contents did not create valid XML... Lambda function will stop. ");
                }
            }
            else if (extension.Equals("json"))
            {
                try
                {
                    Console.WriteLine("Attempting to parse JSON contents... ");
                    JObject jsonOut = JObject.Parse(input);
                    if (jsonOut["id"] != null)
                    {
                        output = new EntryRow();
                        output.id = int.Parse((string)jsonOut["id"]);
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
                        if (jsonOut["tests"] != null) {
                            JArray tests = jsonOut["tests"].Value<JArray>();
                            if (tests != null) {
                                foreach (JToken test in tests) {
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
                        Console.WriteLine("JSON contents are prepped. ");
                    }
                    else
                        Console.WriteLine("JSON contents are invalid... Lambda function will stop. ");
                }
                catch (Newtonsoft.Json.JsonReaderException e)
                {
                    Console.WriteLine("Contents did not create valid JSON... Lambda function will stop. ");
                }
            }
            else
                Console.WriteLine(" extension is an invalid extension. Lambda function will stop. ");
            return output;
        }
    }
}
