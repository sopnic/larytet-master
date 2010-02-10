#! /usr/bin/env python


import threading, time

class ProcessCli(threading.Thread):
    def __init__(self):
        threading.Thread.__init__(self)
    def run(self):
        while (1):
            print 'Another sec'
            time.sleep(1);


print 'Create CLI'
processCli = ProcessCli()
print 'Start CLI'
processCli.start()
print 'Wait for CLI to exit'
processCli.join()    # Wait for the background task to finish

