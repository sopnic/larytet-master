#! /usr/bin/env python

import threading, time, cmd


class CliMain(cmd.Cmd):
    def __init__(self):
        cmd.Cmd.__init__(self)
        use_rawinput = 0
    def do_exit(self, args):
        exit()
    

class ProcessCli(threading.Thread):
    def __init__(self):
        threading.Thread.__init__(self)
        self.cliCmd = CliMain()
    def run(self):
        while (1):
            cmd = self.cliCmd.cmdloop()
            if cmd == 'exit': break


print 'Create CLI'
processCli = ProcessCli()
print 'Start CLI'
processCli.start()
processCli.join()    # Wait for the background task to finish

