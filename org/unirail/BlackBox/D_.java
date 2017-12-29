package org.unirail.BlackBox;

import java.lang.annotation.ElementType;
import java.lang.annotation.Target;

@Target(ElementType.FIELD)
//Optional multidimensional array field. Dimensions are described this way (3|3|4)
public @interface D_ {
	long value();
}
	