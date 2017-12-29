package org.unirail.BlackBox;

import java.lang.annotation.ElementType;
import java.lang.annotation.Target;

//Required multidimensional array field. Dimensions are described this way (3|3|4)
@Target(ElementType.FIELD)
public @interface D {
	long value();
}
	