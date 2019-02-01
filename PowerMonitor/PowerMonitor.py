import argparse
from threading import Thread, Lock, Condition
import serial
import sys
import signal
import logger

SIGINT = False


def signal_handler(sig, frame):
    global SIGINT
    SIGINT = True


signal.signal(signal.SIGINT, signal_handler)


class PowerData:
    def __init__(self):
        """ it is expecting this format:
        Bus Voltage:   5.444 V
        Shunt Voltage: -0.04 mV
        Current:       0 mA
        Power:         0 mW
        """
        self.columns = []
        self.units = []
        self.data = []
        self.log = log

    def parse(self, line):
        if len(self.columns) > 0 and line.strip() == '':
            return True

        parts = line.split(':')
        if len(parts) > 1:
            label = parts[0]
            values = parts[1].strip().split(' ')
            if len(values) == 2:
                self.columns += [label]
                self.data += [values[0]]
                self.units += [values[1]]

        return False


class PowerMonitor:
    """ This class wraps the serial library and reads the input on a background thread
    so the main thread can consume the data and whatever speed it would like to """

    def __init__(self):
        self.read_buffer = []
        self.lock = Lock()
        self.cv = Condition(self.lock)
        self.closed = False
        self.read_thread = None
        self.error = None

    def open(self, serial_port=None, baud_rate=115200):
        """ Open the serial port and start reading on background thread:
        serial_port - serial port to read from
        baud_rate - default is 115200
        """
        self.serial_port = serial_port
        self.baud_rate = baud_rate
        self.stdin_thread = Thread(target=self.read_input, args=())
        self.stdin_thread.daemon = True
        self.stdin_thread.start()

    def read_input(self):
        global SIGINT
        entry = PowerData()
        try:
            with serial.Serial(self.serial_port, self.baud_rate) as ser:
                while not self.closed:
                    if SIGINT:
                        self.closed = True
                        break
                    value = ser.readline().decode("utf-8").strip()
                    if entry.parse(value):
                        # protect access to the shared state
                        self.cv.acquire()
                        try:
                            self.read_buffer += [entry]
                            if len(self.read_buffer) == 1:
                                self.cv.notify()
                        except Exception as e:
                            self.error = e
                            break
                        self.cv.release()
                        entry = PowerData()
        except Exception as e:
            self.error = e

        if len(self.read_buffer) == 1:
            self.cv.notify()

    def read(self):
        """ Read the next PowerData. """
        while not self.closed:
            # block until data is ready...
            result = None
            self.cv.acquire()
            try:
                if self.error:
                    raise self.error
                while len(self.read_buffer) == 0:
                    if self.closed:
                        return None
                    if self.error:
                        raise self.error
                    self.cv.wait(0.1)
                result = self.read_buffer.pop(0)
            except:  # noqa E722
                pass
            self.cv.release()

            if result is not None:
                return result
        return None

    def close(self):
        """ Close the serial port """
        self.closed = True

    def is_closed(self):
        """ return true if the serial port is closed """
        return self.closed


if __name__ == "__main__":
    parser = argparse.ArgumentParser("Log serial input from DigitalWattmeter")
    parser.add_argument("--port", "-p", help="Name of serial port")
    parser.add_argument("--baud_rate", "-r", help="The baud rate (default 115200)", default=115200, type=int)

    logger.add_logging_args(parser)
    args = parser.parse_args()
    log = logger.setup(args)

    if not args.port:
        log.error("Missing --port argument")
        sys.exit(1)

    first = True
    monitor = PowerMonitor()
    monitor.open(args.port, args.baud_rate)
    while True:
        row = monitor.read()
        if row is None:
            break
        if first:
            first = False
            headers = ["{}({})".format(row.columns[i], row.units[i]) for i in range(len(row.columns))]
            log.info(",".join(headers))
        log.info(",".join(row.data))
