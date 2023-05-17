import vgamepad as vg
import time

gamepad = vg.VX360Gamepad()

BUTTON_A = vg.XUSB_BUTTON.XUSB_GAMEPAD_A
BUTTON_B = vg.XUSB_BUTTON.XUSB_GAMEPAD_B
BUTTON_Y = vg.XUSB_BUTTON.XUSB_GAMEPAD_Y
BUTTON_X = vg.XUSB_BUTTON.XUSB_GAMEPAD_X
DPAD_UP = vg.XUSB_BUTTON.XUSB_GAMEPAD_DPAD_UP
DPAD_DOWN = vg.XUSB_BUTTON.XUSB_GAMEPAD_DPAD_DOWN
DPAD_LEFT = vg.XUSB_BUTTON.XUSB_GAMEPAD_DPAD_LEFT
DPAD_RIGHT = vg.XUSB_BUTTON.XUSB_GAMEPAD_DPAD_RIGHT


def tap_button(button, delay=0.1):
    gamepad.press_button(button=button)
    gamepad.update()
    time.sleep(delay)
    gamepad.release_button(button=button)
    gamepad.update()
    time.sleep(0.1)


def left_joystick(x_value, y_value, seconds=1):
    gamepad.left_joystick_float(x_value_float=x_value, y_value_float=y_value)
    gamepad.update()
    time.sleep(seconds)
    gamepad.left_joystick_float(x_value_float=0, y_value_float=0)
    gamepad.update()
    time.sleep(0.1)


def right_joystick(x_value, y_value, seconds=1):
    gamepad.right_joystick_float(x_value_float=x_value, y_value_float=y_value)
    gamepad.update()
    time.sleep(seconds)
    gamepad.right_joystick_float(x_value_float=0, y_value_float=0)
    gamepad.update()
    time.sleep(0.1)


def left_shoulder(delay=0.1):
    gamepad.press_button(button=vg.XUSB_BUTTON.XUSB_GAMEPAD_LEFT_SHOULDER)
    gamepad.update()
    time.sleep(delay)
    gamepad.release_button(button=vg.XUSB_BUTTON.XUSB_GAMEPAD_LEFT_SHOULDER)
    gamepad.update()
    time.sleep(0.1)


def right_shoulder(delay=0.1):
    gamepad.press_button(button=vg.XUSB_BUTTON.XUSB_GAMEPAD_RIGHT_SHOULDER)
    gamepad.update()
    time.sleep(delay)
    gamepad.release_button(button=vg.XUSB_BUTTON.XUSB_GAMEPAD_RIGHT_SHOULDER)
    gamepad.update()
    time.sleep(0.1)


def left_trigger(amount=0.5, delay=0.1):
    gamepad.left_trigger_float(value_float=amount)
    gamepad.update()
    time.sleep(delay)
    gamepad.left_trigger_float(value_float=0)
    gamepad.update()
    time.sleep(0.1)


def right_trigger(amount=0.5, delay=0.1):
    gamepad.right_trigger_float(value_float=amount)
    gamepad.update()
    time.sleep(delay)
    gamepad.left_trigger_float(value_float=0)
    gamepad.update()
    time.sleep(0.1)


def attack():
    # do a x x x x attack sequence that triggers the special attack moves.
    for i in range(6):
        tap_button(BUTTON_X, delay=0.1)


# press a button to wake the device up
tap_button(BUTTON_A)

# press a button to clear "please connect your controller"
tap_button(BUTTON_A)

# do a 4 x x x x kill sequence.
attack()

# go forwards for 1 second
left_joystick(0, 1, 1)
