Welcome to "Easy Cheat":

Here are the steps you need to follow to make it work

1.) Import Unity's new Input system in your Project
2.) Setup a Unity Event System in your scene rightclick in Hierachy->UI-> Event System
3.) Youre ready to go

//How to implement your own Cheats
Make a function and add the CheatCommand Atrribute to it.
Thats it :D

Supported Parameter types are all base types bool,string,int,double,float,uint,long,ulong
All return parameters should be supported if you want your own classes to be supported as a return value that can be displayed in a string override the ToString() method in your class.