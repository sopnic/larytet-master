#! /usr/bin/env python

import threading, time, cmd


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
        exit()
    def help_exit(self):
        print "Quit application"

    def do_sleep(self, args):
        time.sleep(float(args)/1000)
    def help_sleep(self):
        print "Sleep for specified number of milliseconds"

    def do_set(self, arg):
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
          break;
      

    def help_set(self):
        print "Setup connection parameters"

    def do_rd(self, args):
        self._notimplemented()
    def help_rd(self):
        print "Read register"

    def do_wr(self, args):
        self._notimplemented()
    def help_wr(self):
        print "Write register"

    def do_none(self, args):
        self._notimplemented()
    def help_none(self, args):
        print "Do nothing command"

    def _notimplemented(self):
        print 'Command not implemented'
# endof class CliMain



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

