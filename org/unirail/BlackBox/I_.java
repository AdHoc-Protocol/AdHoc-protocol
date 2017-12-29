package org.unirail.BlackBox;

import java.lang.annotation.ElementType;
import java.lang.annotation.Target;

@Target(ElementType.FIELD)
//Optional field with even values distribution in a range of datatype or in provided range (-3 | 82)
public @interface I_ {
	double value() default 0;
}
	