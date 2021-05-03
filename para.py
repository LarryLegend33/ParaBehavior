import pyb
import time

shock = pyb.Pin('X8',pyb.Pin.OUT_PP)
#motor1 = pyb.Pin('X4',pyb.Pin.OUT_PP)
#motor2 = pyb.Pin('Y10',pyb.Pin.OUT_PP)
motor1 = pyb.Pin('X4')
motor2 = pyb.Pin('Y10')
m1_tim = pyb.Timer(9, freq=1000)
m2_tim = pyb.Timer(2, freq=1000)
m1_ch = m1_tim.channel(2, pyb.Timer.PWM, pin=motor1)
m2_ch = m2_tim.channel(4, pyb.Timer.PWM, pin=motor2)

def set_m1(duty_cycle):
    m1_ch.pulse_width_percent(duty_cycle)

def set_m2(duty_cycle):
    m2_ch.pulse_width_percent(duty_cycle)


def shock_para(duration):
	shock.high()
	time.sleep_ms(duration)
	shock.low()

def motor_buzz(duration, intensity):
	set_m1(intensity)
	set_m2(intensity)
	time.sleep_ms(duration)
	set_m1(0)
	set_m2(0)