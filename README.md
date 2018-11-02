Werewolf Server
==================
Werewolf game server, working with Werewolf API https://github.com/cpe200-161/WerewolfAPI . You can test with with the simple Werewolf Client https://github.com/cpe200-161/WerewolfClient or any REST api tester, such as Postman (https://www.getpostman.com/ , see test/ folder for Postman test configuration) 

Required software
-----------------
1. .Net Core 2.0/2.1 https://www.microsoft.com/net/core
2. Visual Studio Code https://code.visualstudio.com/ (optional)

Installation (withtout Visual Studio Code on Windows)
------------
1. Fork the code, then clone to your harddisk
2. Open a Powershell (from Start menu) then go to the cloned folder
3. Run "dotnet build"  to build the project
4. If no error, run "dotnet run" to run the server. 

Installation (withtout Visual Studio Code on Mac/Linux)
------------
1. Fork the code, then clone to your harddisk
2. Open a terminal then go to the cloned folder
3. Run "dotnet build"  to build the project
4. If no error, run "dotnet run" to run the server. 


Installation (With Visual Studio Code on Windows/Mac/Linux)
------------
1. Fork the code, then clone to your harddisk
2. Open the folder in Visual Studio Code
3. Run the project from the menu (Debug->Start Debugging) or from F5 shortcut.


Note
----
1. Now, it is configured to use sqlite database ( [see Configuration file](https://github.com/cpe200-161/WerewolfServer/blob/master/Werewolf/WerewolfContext.cs) ) , you can connect it to local MySQL/MariaDB server for performance, but it's not necessary for testing.
2. You can set the number of users in a game in [here](https://github.com/cpe200-161/WerewolfServer/blob/master/Werewolf/WerewolfGame.cs#L71), but make sure it's either 2, 14, 15 or 16. 2 is the best for testing.
3. When you run the server (e.g., but loading the project to Visual Studio Code, and press F5), you should be able to connect to it in the same computer with URL http://localhost:2343/werewolf/ .
4. No chating system, yet.

Please make sure you fork the git repo first, don't try to push to my repo!
