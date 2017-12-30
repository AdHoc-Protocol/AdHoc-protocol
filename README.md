# BlackBox description file
BackBox is a low level binary protocol boilerplate multilanguage code generator. According to your description, BlackBox generate code, you just need insert you handler on received packages, or create a packege, populate with data and send to receiver. Basic documentation of the description file format can be found **[here](http://www.unirail.org/?lang=ru).** Let's take a look how [**LedBlinkProject** demo description file](https://github.com/cheblin/BlackBox_LEDBlink_Demo/blob/master/org/unirail/demo/LedBlink.java) looks like
![descriptionscheme](http://www.unirail.org/wp-content/uploads/2017/12/Capture2.png)

# BlackBox parts relationship scheme

![description scheme](http://www.unirail.org/wp-content/uploads/2017/12/Schem.png)

# How to get generated code

1. Install **Intellij** [IDEA](https://www.jetbrains.com/idea/download/#section=windows) or if you are planning to deploy your code on Android devices, [Android Studio.](https://developer.android.com/studio/index.html)
2. Download [BlackBox metadata annotations](https://github.com/cheblin/BlackBox/tree/master/org/unirail/BlackBox)
3. Create new Project in the IDE and make reference to downloaded metadata annotations.
3. Compose your protocol description file (use UTF8 encoding).
4. When you finish, ensure that description file can me compiled without any errors.
5. Attach you description file to th email and send it to the address **OneBlackBoxPlease@outlook.com**
6. Get your code in reply.
