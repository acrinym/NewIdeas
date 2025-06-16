# Android JSON Viewer/Editor Tool

This guide outlines how to create a simple Android application capable of viewing and searching JSON files exported from ChatGPT or similar services. A minimal implementation is provided in the `android-json-tool/` folder of this repository. You can open that directory in Android Studio and build the APK directly.

The tool will:

- Open and parse JSON files even if they contain minor formatting differences.
- Search across all nested values and keys.
- Allow editing of text fields with a simple interface.
- Export selected text portions to readable `.txt` files.

## Getting Started

1. **Install Android Studio**
   - Download the latest version of Android Studio for your platform.
   - Ensure that the Android SDK and build tools are also installed.

2. **Open the Provided Project**
   - In Android Studio, choose **Open** and select the `android-json-tool` directory.
   - The project already contains the required configuration and dependencies.

3. **Add Dependencies**
   - Add the [Gson](https://github.com/google/gson) library for JSON parsing in your app-level `build.gradle`:

     ```gradle
     dependencies {
         implementation 'com.google.code.gson:gson:2.10.1'
     }
     ```

4. **Layout**
   - Use a `RecyclerView` to display key-value pairs.
   - Provide a search bar at the top to filter across all nodes.

5. **Parsing Flexible JSON**
   - Some exports may not be perfectly formatted. Use `Gson` with `JsonParser.parseString` in a try/catch block to handle minor issues.

   ```kotlin
   val jsonElement = try {
       JsonParser.parseString(rawText)
   } catch (ex: Exception) {
       // Attempt to fix common issues like missing commas
       val sanitized = rawText.replace("\n", " ")
       JsonParser.parseString(sanitized)
   }
   ```

6. **Searching the Structure**
   - Traverse the JSON tree recursively and collect matches. The function below searches every value and key:

   ```kotlin
   fun searchJson(element: JsonElement, query: String, matches: MutableList<String>, path: String = "") {
       when {
           element.isJsonObject -> {
               for ((k, v) in element.asJsonObject.entrySet()) {
                   val newPath = if (path.isEmpty()) k else "$path.$k"
                   if (k.contains(query, ignoreCase = true)) matches.add(newPath)
                   searchJson(v, query, matches, newPath)
               }
           }
           element.isJsonArray -> {
               val arr = element.asJsonArray
               arr.forEachIndexed { i, sub ->
                   val newPath = "$path[$i]"
                   searchJson(sub, query, matches, newPath)
               }
           }
           element.isJsonPrimitive -> {
               val text = element.asJsonPrimitive.toString()
               if (text.contains(query, ignoreCase = true)) matches.add("$path: $text")
           }
       }
   }
   ```

7. **Editing Fields**
   - Display editable text boxes for primitive values. After editing, update the underlying `JsonElement` and re-serialize it with `Gson` to save changes.

8. **Exporting Text**
   - When the user selects a path, extract the value and write it to a `.txt` file using Android's file API:

   ```kotlin
   fun exportText(context: Context, text: String, filename: String) {
       context.openFileOutput(filename, Context.MODE_PRIVATE).use { stream ->
           stream.write(text.toByteArray())
       }
   }
   ```

## Building the APK

1. **Compile and Test**
   - Connect an Android device or use an emulator in Android Studio.
   - Click **Run** to build and deploy the APK.

2. **Generate Release APK**
   - In Android Studio, select **Build → Generate Signed Bundle / APK**.
   - Follow the prompts to create a signing key and generate the release APK.

## Tips

- Keep the interface simple, especially when displaying deeply nested JSON.
- Provide an option to pretty-print or collapse sections for readability.
- For large files, consider streaming rather than loading the entire JSON into memory.

