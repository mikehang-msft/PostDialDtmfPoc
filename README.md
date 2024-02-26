# PostDialDtmfPoc

**Source Code Repository**
Open a terminal or command prompt, and cd into a folder where you would like to clone this repo. Then run:
1.	git clone https://github.com/mikehang-msft/PostDialDtmfPoc
2.	cd PostDialDtmfPoc
3.	dotnet build

**Code Structure**
- [ ] src/Web.API/Program.cs: An entry point to the start the Web.API. This class also exposes a few endpoints used to handle call event callbacks, add participant to an existing call or send DTMF tones to a participant.
- [ ] src/Web.API/WebApplicationExtensions.cs: This class exposes additional endpoints to assist with rooms operations like create a new room with participants. 
- [ ] src/Web.API/AnswerCallWorker.cs: This is background service which continuously searches and processes messages by listening to Azure Storage Queue. This queue is set up to store incoming call event messages enqueued by an event grid, which is configured in the ACS resource.
- [ ] src/Web.API/appsettings.json: This is an appsettings storing essential config data like connection string and PSTN phone number, etc. See configuration data section below for details.

**Configuration Data**
- [ ] _Acs:ConnectionString_: specify connection string to ACS Resource used in this POC.
- [ ] _Acs:CallbackUri_: specify URI that Call Automation client will execute a callback via the URI after accepting the call. This piece of code is implemented in AnswerCallWorker.HandleMessage() to handle dialing out to PSTN number and sending DTMF tones. In this POC, we are using Dev Tunnel to generate a public callback URI. Please refer to the following article for configuring Dev Tunnels in Visual Studio 2022.
- [ ] _Storage.ConnectionString_: specify connection string Azure Storage Queue.
- [ ] _PhoneNumbers.Target_: specify a valid target phone number.
- [ ] _PhoneNumbers.CallerId_: specify phone number provisioned in the ACS resource.

**Run projects**
With appsettings.json filled with required config data. Hit F5 to run the POC in Visual Studio. 
