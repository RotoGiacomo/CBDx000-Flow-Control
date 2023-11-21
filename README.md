# CBDx000-Flow-Control

This is an example of how to manage flow control without using the XON-XOFF protocol on Rototype CBD1000 and CBD2000 machines.

# Scope

The Venerable CBD1000 was born equipped with three simple needles printers and the XON-XOFF flow control was more than enough to transfer some text strings. 
The upgrade to ink-jet printers and base64 data transfer multiplied the data to be transferred and revealed a weakness in the data transfer protocol.
The XON-XOFF protocol works as long as the machine is able to consume the data; in the event of a jam, the buffer remains full and the host cannot send the order to resume execution (@R) or to empty the buffer (@C).

There are two ways to work around this issue: Either manage the XON-XOFF protocol at the application level or count the cheques printed and only send enough of them to leave the machine without data to produce a new cheque, but not enough to fill the receive buffer.
We have adopted the latter solution.

#How to execute

The application is designed to work in debug mode (the print data is in the 'Debug' folder). The serial port must be chosen in accordance with your hardware configuration. 


