package org.unirail.BlackBox;

import java.lang.annotation.ElementType;
import java.lang.annotation.Target;

//Required bit field.
@Target(ElementType.FIELD)
public @interface B {
	long value();//how many bits it used or values range (3|82)
}
	