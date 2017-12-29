package org.unirail.BlackBox;

import java.lang.annotation.ElementType;
import java.lang.annotation.Target;

//Optional(nullable) field that value has has up direction values dispersion gradient.
@Target(ElementType.FIELD)
public @interface A_ {
	double value() default 0;//most often lowest value or value range (-7 | 78)
}
	