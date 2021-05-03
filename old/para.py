import pyb
import time

shock = pyb.Pin('X8',pyb.Pin.OUT_PP)
motor1 = pyb.Pin('X4',pyb.Pin.OUT_PP)
motor2 = pyb.Pin('Y10',pyb.Pin.OUT_PP)

def shock_para(duration):
	shock.high()
	time.sleep_ms(duration)
	shock.low()

def move_para(duration):
	motor1.high()
	motor2.high()
	time.sleep_ms(duration)
	motor1.low()
	motor2.low()