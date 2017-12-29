package org.unirail.BlackBox;

import java.lang.annotation.ElementType;
import java.lang.annotation.Target;

//Pack unique identification. Applied by codogenerator.
@Target(ElementType.TYPE)
public @interface id {
	long value();
}
