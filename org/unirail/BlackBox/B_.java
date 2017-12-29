package org.unirail.BlackBox;

import java.lang.annotation.ElementType;
import java.lang.annotation.Target;

//Optional(nullable) bit field.
@Target(ElementType.FIELD)
public @interface B_ {
	long value();//how many bits it used or values range (3|82)
}
	