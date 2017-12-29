package org.unirail.BlackBox;

import java.lang.annotation.ElementType;
import java.lang.annotation.Target;

@Target(ElementType.FIELD)
// Required field that value has down direction values dispersion gradient.
public @interface V {
	double value() default 0;//most often highest  value or value range (-7 | 78)
}
	