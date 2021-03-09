import pyb
import time

shock = pyb.Pin('Y10',pyb.Pin.OUT_PP)

def shock_para(duration):
	shock.high()
	time.sleep_ms(duration)
	shock.low()