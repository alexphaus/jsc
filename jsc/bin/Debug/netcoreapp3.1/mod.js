#!stdlib
import mscorlib;

x = 5;

cil mul(int, int) int
{
	ldarg_0;
	ldarg_1;
	mul;
	ret;
}

cil add(Object[]) Object
{
	ldarg_ref 0;
	unbox_any int;
	ldarg_ref 1;
	unbox_any int;
	add;
	box int;
	ret;
}

function add2(a, b) {
	return a + b
}

echo add(1,2);

for (i = 0; i < 10000000; i++) {
	mul(1, 2)
}
