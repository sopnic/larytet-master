#! /usr/bin/env python


# This is a simple command line application allowing to send commands
# to the serial device and read the data from the serial device
# Read is done in a separate thread
#

import threading, time, cmd
import serial

# A thread reading and printing received data 
class SerialPort(threading.Thread):
    def __init__(self, device, rate):
        threading.Thread.__init__(self)
        self.device = device
        self.rate = rate
        self.exitflag = False
        print 
        try:
            self.tty = serial.Serial(device, rate, timeout=0.300, bytesize=8, parity='N', stopbits=1, xonxoff=0, rtscts=0)
            self.tty.open()
            self.exitflag = not self.tty.isOpen()
        except serial.SerialException:
            print "Could not connect to the serial device ", device
            self.exitflag = True;

    def isconnected(self):
        return not self.exitflag;
     
    def run(self):
        while (1):
            if (self.exitflag):
                print "Disconnected"
                break
            try:
                str = self.tty.read(10)
            except Exception:
                self.exitflag = True;
            if (str != ""):
                print str

    def close(self):
            if (not self.exitflag):
                self.tty.close()
            self.exitflag = True;

    def write(self, str):
            try:
                str = self.tty.write(str)
            except Exception:
                self.exitflag = True;            

    def writeLn(self, str):
            self.write(str)
            self.write('\r\n')
# endof class SerialPort

# all CLI commands are here
class CliMain(cmd.Cmd):
    def __init__(self):
        cmd.Cmd.__init__(self)
        use_rawinput = False

    def help_help(self):
        print "Display this help"

    def do_quit(self, args):
        self.do_exit(args)
    def help_quit(self):
        self.help_exit()

    def do_exit(self, args):
        if (self.serialPort):
            self.serialPort.close()
        exit()
    def help_exit(self):
        print "Quit application"

    def do_sleep(self, args):
        time.sleep(float(args)/1000)
    def help_sleep(self):
        print "Sleep for specified number of milliseconds"

    #Example: conn /dev/ttyUSB4 115200
    def do_conn(self, arg):
       args = arg.rsplit(" ");
       device = args[0]
       rate = args[1]
       while True:
          if (not rate.isdigit()): 
              print "Wrong rate ", rate
              break
          self.device = args[0];
          self.rate = int(args[1]);
          print "Serial device ", self.device, ", rate ", self.rate
          if (hasattr(self, "serialPort") and self.serialPort.isconnected()):     #close previous connection
              self.serialPort.close()
          self.serialPort = SerialPort(self.device, self.rate);
          if (self.serialPort.isconnected()):
              self.serialPort.start();
          break;
      
    def help_conn(self):
        print "Connect serial device [device, rate]"

    def do_disc(self, arg):
        self.serialPort.stop()

    def help_disc(self):
        print "Disconnect serial device"


    def do_rd(self, args):
        while (True):
            if (not hasattr(self, "serialPort") or not self.serialPort.isconnected()):
                print "Serial device is not connected"
                break

            argsArray = args.rsplit(" ");
            address = argsArray[0];
            address = address.zfill(8)                           # add zeros until i get 8 characters
            print "Read address ", address
            self.serialPort.writeLn("rd " + address)
            break;

    def help_rd(self):
        print "Read register"

    def do_wr(self, args):
        while (True):
            if (not hasattr(self, "serialPort") or not self.serialPort.isconnected()):
                print "Serial device is not connected"
                break

            argsArray = args.rsplit(" ");
            address = argsArray[0]
            address = address.zfill(8)
            value = argsArray[1]
            value = value.zfill(8)
      
            self.serialPort.writeLn("wr " + address + " " + value)
            break;


    def help_wr(self):
        print "Write register"

    def do_none(self, args):
        self._notimplemented()
    def help_none(self, args):
        print "Do nothing command"

    def _notimplemented(self):
        print 'Command not implemented'

# endof class CliMain



# loop until exit command is not received
class ProcessCli(threading.Thread):
    def __init__(self):
        threading.Thread.__init__(self)
        self.cliCmd = CliMain()
    def run(self):
        while (1):
            cmd = self.cliCmd.cmdloop()
            if cmd == 'exit': break
# endof class ProcessCli


print 'Create CLI'
processCli = ProcessCli()
print 'Start CLI'
processCli.start()
processCli.join()    # Wait for the background task to finish

