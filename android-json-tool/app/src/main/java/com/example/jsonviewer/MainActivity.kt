package com.example.jsonviewer

import android.app.Activity
import android.content.Context
import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.text.InputType
import android.widget.Button
import android.widget.EditText
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.google.gson.JsonElement
import com.google.gson.JsonParser
import java.io.BufferedReader

class MainActivity : AppCompatActivity() {
    private var rootJson: JsonElement? = null
    private val results = mutableListOf<Result>()
    private lateinit var adapter: ResultsAdapter

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        adapter = ResultsAdapter(results) { result ->
            showValueDialog(result)
        }

        findViewById<RecyclerView>(R.id.resultsList).apply {
            layoutManager = LinearLayoutManager(this@MainActivity)
            adapter = this@MainActivity.adapter
        }

        findViewById<Button>(R.id.openJsonButton).setOnClickListener {
            openFile()
        }

        findViewById<Button>(R.id.searchButton).setOnClickListener {
            val query = findViewById<EditText>(R.id.searchInput).text.toString()
            search(query)
        }
    }

    private fun openFile() {
        val intent = Intent(Intent.ACTION_OPEN_DOCUMENT).apply {
            addCategory(Intent.CATEGORY_OPENABLE)
            type = "application/json"
        }
        startActivityForResult(intent, 100)
    }


    private fun readJson(uri: Uri) {
        val text = contentResolver.openInputStream(uri)?.bufferedReader()?.use(BufferedReader::readText)
        text?.let { raw ->
            rootJson = try {
                JsonParser.parseString(raw)
            } catch (_: Exception) {
                val sanitized = raw.replace("\n", " ")
                JsonParser.parseString(sanitized)
            }
            results.clear()
            adapter.update(emptyList())
        }
    }

    private fun search(query: String) {
        val root = rootJson ?: return
        val matches = mutableListOf<Result>()
        searchJson(root, query, matches)
        adapter.update(matches)
    }

    private fun searchJson(element: JsonElement, query: String, matches: MutableList<Result>, path: String = "") {
        when {
            element.isJsonObject -> {
                for ((k, v) in element.asJsonObject.entrySet()) {
                    val newPath = if (path.isEmpty()) k else "$path.$k"
                    if (k.contains(query, ignoreCase = true)) matches.add(Result(newPath))
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
                if (text.contains(query, ignoreCase = true)) matches.add(Result("$path: $text"))
            }
        }
    }

    private fun showValueDialog(result: Result) {
        val root = rootJson ?: return
        val value = getValueAtPath(root, result.path)
        val input = EditText(this).apply {
            setText(value)
            inputType = InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_FLAG_MULTI_LINE
        }
        AlertDialog.Builder(this)
            .setTitle(result.path)
            .setView(input)
            .setPositiveButton("Save") { _, _ ->
                updateValueAtPath(root, result.path, input.text.toString())
            }
            .setNegativeButton("Export") { _, _ ->
                exportText(input.text.toString())
            }
            .setNeutralButton("Close", null)
            .show()
    }

    private fun getValueAtPath(element: JsonElement, path: String): String {
        var current: JsonElement = element
        val segments = path.split(".")
        for (seg in segments) {
            current = when {
                seg.contains("[") -> {
                    val name = seg.substringBefore("[")
                    val index = seg.substringAfter("[").substringBefore("]").toInt()
                    current.asJsonObject.get(name).asJsonArray[index]
                }
                else -> current.asJsonObject.get(seg)
            }
        }
        return current.toString()
    }

    private fun updateValueAtPath(element: JsonElement, path: String, newValue: String) {
        var parent: JsonElement = element
        val segments = path.split(".")
        for (i in 0 until segments.size - 1) {
            val seg = segments[i]
            parent = if (seg.contains("[") ) {
                val name = seg.substringBefore("[")
                val index = seg.substringAfter("[").substringBefore("]").toInt()
                parent.asJsonObject.get(name).asJsonArray[index]
            } else {
                parent.asJsonObject.get(seg)
            }
        }
        val last = segments.last()
        if (parent.isJsonObject) {
            parent.asJsonObject.addProperty(last, newValue)
        }
    }

    private fun exportText(text: String) {
        val intent = Intent(Intent.ACTION_CREATE_DOCUMENT).apply {
            addCategory(Intent.CATEGORY_OPENABLE)
            type = "text/plain"
            putExtra(Intent.EXTRA_TITLE, "export.txt")
        }
        startActivityForResult(intent, 101)
        exportPending = text
    }

    private var exportPending: String? = null

    private fun writeExport(uri: Uri) {
        exportPending?.let { text ->
            contentResolver.openOutputStream(uri)?.use { stream ->
                stream.write(text.toByteArray())
            }
            exportPending = null
        }
    }

    override fun onActivityResult(requestCode: Int, resultCode: Int, data: Intent?) {
        if (requestCode == 100 && resultCode == Activity.RESULT_OK) {
            data?.data?.also { uri -> readJson(uri) }
            return
        }
        if (requestCode == 101 && resultCode == Activity.RESULT_OK) {
            data?.data?.also { uri -> writeExport(uri) }
            return
        }
        super.onActivityResult(requestCode, resultCode, data)
    }
}
