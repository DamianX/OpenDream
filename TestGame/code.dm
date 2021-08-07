/turf
	icon = 'icons/turf.dmi'
	icon_state = "turf"

/turf/blue
	icon_state = "turf_blue"

/mob
	icon = 'icons/mob.dmi'
	icon_state = "mob"

	New()
		..()
		loc = locate(5, 5, 1)

	verb/tell_location()
		usr << "You are at ([x], [y], [z])"

	verb/say(message as text)
		var/list/viewers = viewers()

		for (var/mob/viewer in viewers)
			viewer << "[ckey] says: \"[message]\""

	verb/say_loud()
		var/msg = input("Please put the message you want to say loudly.", "Say Loud", "Hello!")
		world << "[ckey] says loudly: \"[msg]\""

	verb/move_up()
		step(src, UP)

	verb/move_down()
		step(src, DOWN)

	verb/roll_dice(dice as text)
		var/result = roll(dice)
		usr << "The total shown on the dice is: [result]"

/mob/Stat()
	statpanel("Status", "CPU: [world.cpu]")

/world/New()
	..()
	world.log << "World loaded!"
	var/matrix/M = matrix(1,2,3,4,5,6)
	var/matrix/N = matrix(7,8,9,10,11,12)
	M.Multiply(N)
	world.log << "[M.a] [M.b] [M.c] [M.d] [M.e] [M.f]"
