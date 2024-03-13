# PostDialDtmfPoc
**Overview**

This proof of concept (POC) is aimed to demonstrates two capabilities 
1. Ability to dial out to a PSTN number and then follow up with DTMF tones 
2. Ability to announce to a PSTN participant when they get muted and unmute PSTN user using a back-end service when they press *6. This POC leverages ACS Rooms call and Call Automation capabilities. 

The following components are used to run a full end to end workflows: 
1. PostDialDtmfPOC Service (this project): A server-side service listens for incoming call events and triggers call automation client to dial out to a PSTN number, sends a DTMF tones and handles mute/unmute for the remote PSTN user. 
2. ACS Web Calling SDK app: this a sample client-side calling app allows participants to join a Room call and add other participants into the call. This app is available to public and can be found in [this tutorial](https://learn.microsoft.com/en-us/samples/azure-samples/communication-services-web-calling-tutorial/acs-calling-tutorial/). 

**Prerequisites**
- [ ] Visual Studio 2022 or Visual Source Code 1.86.0 or higher 
- [ ] .NET 8 or higher 

**Source Code Repository**
Open a terminal or command prompt, and cd into a folder where you would like to clone this repo. Then run:
1.	git clone https://github.com/mikehang-msft/PostDialDtmfPoc
2.	cd PostDialDtmfPoc
3.	dotnet build

**Code Structure**
- src/Web.API/Program.cs: An entry point to the start the Web.API. This class also exposes a few endpoints used to handle call event callbacks, add participant to an existing call or send DTMF tones to a participant.
- src/Web.API/WebApplicationExtensions.cs: This class exposes additional endpoints to assist with rooms operations like create a new room with participants. 
- src/Web.API/AnswerCallWorker.cs: This is background service which continuously searches and processes messages by listening to Azure Storage Queue. This queue is set up to store incoming call event messages enqueued by an event grid, which is configured in the ACS resource.
- src/Web.API/appsettings.json: This is an appsettings storing essential config data like connection string and PSTN phone number, etc. See configuration data section below for details.

**Configuration Data**
- _Acs:ConnectionString_: specify connection string to ACS Resource used in this POC.
- _Acs:CallbackUri_: specify URI that Call Automation client will execute a callback via the URI after accepting the call. This piece of code is implemented in AnswerCallWorker.HandleMessage() to handle dialing out to PSTN number and sending DTMF tones. In this POC, we are using Dev Tunnel to generate a public callback URI. Please refer to the following article for configuring [Dev Tunnels in Visual Studio 2022](https://learn.microsoft.com/en-us/aspnet/core/test/dev-tunnels?view=aspnetcore-8.0).
- _Storage.ConnectionString_: specify connection string Azure Storage Queue.
- _Storage.QueueName_: specify name of the Azure Storage Queue.
- _PhoneNumbers.Target_: specify a valid target phone number.
- _PhoneNumbers.CallerId_: specify phone number provisioned in the ACS resource.

**Run projects**
1. Fill appsettings.json with required config values
1. Select a Dev Tunnel of which the CallbackUri is used
1. Hit F5 to run the POC in Visual Studio

