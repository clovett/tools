import argparse
import vgamepad as vg
import time
import json


def update_button(gamepad, previous, current, button):
    if not (previous & button) and (current & button):
        print("Pressing button: {}".format(button))
        gamepad.press_button(button=button)
    elif (previous & button) and not (current & button):
        print("Releasing button: {}".format(button))
        gamepad.release_button(button=button)


def play(filename):
    gamepad = vg.VX360Gamepad()
    gamepad.update()

    print("Focus bleeding edge game...")
    time.sleep(3)
    gamepad.press_button(button=vg.XUSB_BUTTON.XUSB_GAMEPAD_A)  # wake up the controller
    time.sleep(0.1)
    gamepad.release_button(button=vg.XUSB_BUTTON.XUSB_GAMEPAD_A)

    time.sleep(3)
    gamepad.press_button(button=vg.XUSB_BUTTON.XUSB_GAMEPAD_A)  # wake up the controller
    time.sleep(0.1)
    gamepad.release_button(button=vg.XUSB_BUTTON.XUSB_GAMEPAD_A)

    print("loading recording...")
    with open(filename, "r") as f:
        recording = json.load(f)

    time.sleep(3)

    print("playing...")
    start = 0
    pb = 0
    print("Playing...")
    for s in recording:
        ticks, blt, brt, slx, sly, srx, sry, buttons = s['ticks'], s['blt'], s['brt'], s['slx'], s['sly'], s['srx'], s['sry'], s['buttons']
        if start == 0:
            start = ticks
        else:
            time.sleep(ticks)  # play back at the same speed
        gamepad.left_trigger(value=blt)  # value between 0 and 255
        gamepad.right_trigger(value=brt)  # value between 0 and 255
        gamepad.left_joystick(x_value=slx, y_value=sly)  # values between -32768 and 32767
        gamepad.right_joystick(x_value=srx, y_value=sry)  # values between -32768 and 32767
        update_button(gamepad, pb, buttons, vg.XUSB_BUTTON.XUSB_GAMEPAD_DPAD_UP)
        update_button(gamepad, pb, buttons, vg.XUSB_BUTTON.XUSB_GAMEPAD_DPAD_DOWN)
        update_button(gamepad, pb, buttons, vg.XUSB_BUTTON.XUSB_GAMEPAD_DPAD_LEFT)
        update_button(gamepad, pb, buttons, vg.XUSB_BUTTON.XUSB_GAMEPAD_DPAD_RIGHT)
        update_button(gamepad, pb, buttons, vg.XUSB_BUTTON.XUSB_GAMEPAD_START)
        update_button(gamepad, pb, buttons, vg.XUSB_BUTTON.XUSB_GAMEPAD_BACK)
        update_button(gamepad, pb, buttons, vg.XUSB_BUTTON.XUSB_GAMEPAD_LEFT_THUMB)
        update_button(gamepad, pb, buttons, vg.XUSB_BUTTON.XUSB_GAMEPAD_RIGHT_THUMB)
        update_button(gamepad, pb, buttons, vg.XUSB_BUTTON.XUSB_GAMEPAD_LEFT_SHOULDER)
        update_button(gamepad, pb, buttons, vg.XUSB_BUTTON.XUSB_GAMEPAD_RIGHT_SHOULDER)
        update_button(gamepad, pb, buttons, vg.XUSB_BUTTON.XUSB_GAMEPAD_A)
        update_button(gamepad, pb, buttons, vg.XUSB_BUTTON.XUSB_GAMEPAD_B)
        update_button(gamepad, pb, buttons, vg.XUSB_BUTTON.XUSB_GAMEPAD_X)
        update_button(gamepad, pb, buttons, vg.XUSB_BUTTON.XUSB_GAMEPAD_Y)
        pb = buttons
        gamepad.update()


def main():
    parser = argparse.ArgumentParser("Play game pad inputs previously recorded.")
    parser.add_argument("--input", "-i", type=str, default="recording.json", help="Input file name.")
    args = parser.parse_args()
    play(args.input)


if __name__ == "__main__":
    main()
