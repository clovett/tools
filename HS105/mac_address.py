
import subprocess
import re

class MacAddress:
    def __init__(self, mac):
        self.mac = mac.lower()

    def lookup_ip_address(self):
        arp = self._exec(["arp", "-a"])
        lines = arp.split('\n')
        for x in lines:
            y = x.lower()
            if self.mac in y:
                parts = re.search('[ ]*([0-9a-f.]+)', y).groups()
                if len(parts) == 1:
                    return parts[0]
        return None

    def _exec(self, args):
            proc = subprocess.Popen(args,
                                    stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                    universal_newlines=True)
            try:
                output, errors = proc.communicate()
            except subprocess.TimeoutExpired:
                proc.kill()
                output, errors = proc.communicate()
                if output:
                    print(output)
                if errors:
                    print(errors)
            return output
