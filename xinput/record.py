import argparse
import XInput
import time
import json


def record(filename, device=0):
    XInput.get_connected()
    recording = []
    dwPacketNumber = None
    start = 0
    print("Recording (press Ctrl+C to stop)...")
    try:
        while True:
            state = XInput.get_state(device)
            if state.dwPacketNumber != dwPacketNumber:
                dwPacketNumber = state.dwPacketNumber
                now = time.time()
                recording.append({
                    'ticks': now - start,
                    'blt': state.Gamepad.bLeftTrigger,
                    'brt': state.Gamepad.bRightTrigger,
                    'slx': state.Gamepad.sThumbLX,
                    'sly': state.Gamepad.sThumbLY,
                    'srx': state.Gamepad.sThumbRX,
                    'sry': state.Gamepad.sThumbRY,
                    'buttons': state.Gamepad.wButtons
                })
                start = now
    except KeyboardInterrupt:
        pass

    with open(filename, "w") as f:
        json.dump(recording, f, indent=2)


def main():
    parser = argparse.ArgumentParser("Record game pad inputs and save them to a file.")
    parser.add_argument("--output", "-o", type=str, default="recording.json", help="Output file name.")
    parser.add_argument("--device", "-d", type=int, default="0", help="GamePad index (default 0).")
    args = parser.parse_args()
    record(args.output, args.device)


if __name__ == "__main__":
    main()
