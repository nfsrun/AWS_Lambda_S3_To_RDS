# AWS_Lambda_S3_To_RDS
## Created By Kevin Tran
## Finished Version: 1.00 - First Complete Upload - 30/04/2019
## CS455 - Project 1
## Professor Alfred Nehme

### Description
A Lambda function used to connect a specific JSON/XML format file and put its contents to RDS. In this scenario, I completed a Lambda function that will connect to an S3 bucket and an RDS Postgres server. It will convert a given JSON or XML with a given format from the S3 bucket and add it to a RDS Postgres table. 

### Version Log
## 1.00 -- First Complete Upload -- 30/04/2019

### Amazon Web Services (AWS) Services Architecture
1. S3 - Established Bucket with Bucket Policy Allowing Role (attached to respective Lambda function) with GET access. 
2. Lambda - Function with attached GET trigger to S3 bucket and connected to a role with full access to S3 and RDS. 
3. RDS - Established with PostgreSQL and attached security group stipulating allowance of incoming connections to port 5432 from RDS users' IP addresses. RDS table must have ID as a non-null primary key in the established table.  
  
### Data Input: 
All XML and JSON files must have an ID present. All XML must have patient as the root node name.  
XML example:  
~~~~
<patient>  
	<id>1</id>  
	<age>62</age>  
	<gender>male</gender>  
	<maritalStatus>married</maritalStatus>  
  <bmi>27.2</bmi>  
	<smoker>no</smoker>  
	<alcoholConsumption>moderate</alcoholConsumption>  
	<tests>  
		<test name="total-cholesterol">220</test>  
		<test name="LDL-cholesterol">65</test>  
		<test name="HDL-cholesterol">45</test>  
		<test name="triglycerides">75</test>  
		<test name="plasmaCeramides">187</test>  
		<test name="natriureticPeptide">133</test>  
	</tests>	  
	<hasVascularDisease>yes</hasVascularDisease>  
</patient>
~~~~  
JSON example:  
~~~~
{
    "id": "6",
    "age": "52",
    "gender": "female",
    "maritalStatus": "single",
    "bmi": "27.2",
    "smoker": "no",
    "alcoholConsumption": "moderate",
    "tests": [
       {
          "name": "total-cholesterol",
          "value": "220"
       },
       {
          "name": "LDL-cholesterol",
          "value": "65"
       },
       {
          "name": "HDL-cholesterol",
          "value": "45"
       },
       {
          "name": "triglycerides",
          "value": "75"
       },
       {
          "name": "plasmaCeramides",
          "value": "187"
       },
       {
          "name": "natriureticPeptide",
          "value": "133"
       }
    ], 
    "hasVascularDisease": "no"
 }
~~~~  
  

### Clarifications on Instructions
#### As of 03/06/2018
This section is made to stipulate certain typos and confusions pertaining to the assignment and its instructions in order to facilitate grading integrity. 

1. N/A
