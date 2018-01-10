**BackBox** is a low level binary protocol boilerplate multilanguage **( JAVA, C, C#...SWIFT(upcoming))** code generator. BlackBox generates source code according to your description, so you just need to insert your received packs handlers, and logic of creating a new package, populating, with data and sending it to the receiver.
# BlackBox description file
Basic documentation of the description file format can be found **[here](http://www.unirail.org).** Let's take a look how [**LedBlinkProject** demo description file](https://github.com/cheblin/BlackBox_LEDBlink_Demo/blob/master/org/unirail/demo/LedBlinkProject.java) looks like
![descriptionscheme](http://www.unirail.org/wp-content/uploads/2017/12/Capture2.png)

# BlackBox parts relationship scheme

![description scheme](http://www.unirail.org/wp-content/uploads/2017/12/Schem2.png)

# How to get generated source code

1. Install **Intellij** [IDEA](https://www.jetbrains.com/idea/download/#section=windows) or, if you are planning to deploy your code on Android devices, [Android Studio.](https://developer.android.com/studio/index.html)
2. Download [BlackBox metadata annotations](https://github.com/cheblin/BlackBox/tree/master/org/unirail/BlackBox)
3. Create a new java Project in your IDE and make reference to downloaded metadata annotations. (On **Android Studio** you have to edit     [build.gradle](https://github.com/cheblin/BlackBox_LEDBlink_Demo/blob/master/Examples/Android/app/build.gradle) file. Find/add **java.srcDirs** option.)
3. Compose your protocol description file (it should be in UTF8 encoding).
4. Ensure that description file can me compiled without any errors.
5. Attach you description file to the email and send it to the address **OneBlackBoxPlease@outlook.com**
6. In a short time getting zipped archive of your generated, fully tested source code in reply. In addition it will contain Demo and Test file, examples of using generated API and one of the passsed test, respectively.
7. For comfortable work with generated java code, please install SlimEnum plugin from Intellij plugin repository or download and install  [SlimEnum.jar](https://github.com/cheblin/SlimEnum) directly.
