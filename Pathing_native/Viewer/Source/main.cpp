#include <iostream>
#include <vector>

#include "gles2.h"
#define GLFW_INCLUDE_NONE
#include <GLFW/glfw3.h>
#include <glm/glm.hpp>
#include <glm/ext.hpp>

#include "Viewer.hpp"
#include "DOLInterface.h"

static void error_callback(int error, char const* description)
{
	std::cerr << "Error: " << description << std::endl;
}

static void key_callback(GLFWwindow* window, int key, int scancode, int action, int mods)
{
	if (key == GLFW_KEY_ESCAPE && action == GLFW_PRESS)
		glfwSetWindowShouldClose(window, GLFW_TRUE);
}

static const char* vertex_shader_text =
	"#version 110\n"
	"uniform mat4 MVP;\n"
	"attribute vec4 vPos;\n"
	"varying vec3 color;\n"
	"void main()\n"
	"{\n"
	"    gl_Position = MVP * vec4(vPos.xyz, 1.0);\n"
	"    color = vec3(vPos.a, vPos.a, 1.0);\n"
	"}\n";

static const char* fragment_shader_text =
	"#version 110\n"
	"varying vec3 color;\n"
	"void main()\n"
	"{\n"
	"    gl_FragColor = vec4(color, 1.0);\n"
	"}\n";

static GLuint init_example()
{
	GLuint vertex_shader, fragment_shader, program;


	vertex_shader = glCreateShader(GL_VERTEX_SHADER);
	glShaderSource(vertex_shader, 1, &vertex_shader_text, NULL);
	glCompileShader(vertex_shader);

	fragment_shader = glCreateShader(GL_FRAGMENT_SHADER);
	glShaderSource(fragment_shader, 1, &fragment_shader_text, NULL);
	glCompileShader(fragment_shader);

	program = glCreateProgram();
	glAttachShader(program, vertex_shader);
	glAttachShader(program, fragment_shader);
	glLinkProgram(program);

	return program;
}

int main(int ac, char** av)
{
	if (ac < 2)
	{
		std::cerr << "Usage: " << av[0] << " <navmesh file>" << std::endl;
		return EXIT_FAILURE;
	}
	dtNavMesh* mesh;
	dtNavMeshQuery* query;
	if (!LoadNavMesh(av[1], &mesh, &query))
	{
		std::cerr << "Can't load navmesh file " << av[1] << std::endl;
		return EXIT_FAILURE;
	}
	auto _navmeshCleaner = RAII([mesh, query] { FreeNavMesh(mesh, query); });

	if (!glfwInit())
	{
		std::cerr << "Can't initialize GLFW" << std::endl;
		return EXIT_FAILURE;
	}
	auto _glfwCleaner = RAII([] {glfwTerminate(); });
	glfwSetErrorCallback(error_callback);

	glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 4);
	glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 2);

	auto window = glfwCreateWindow(1024, 768, "Viewer", nullptr, nullptr);
	if (!window)
		return EXIT_FAILURE;
	auto _windowCleaner = RAII([window] {glfwDestroyWindow(window); });
	glfwMakeContextCurrent(window);
	glfwSetKeyCallback(window, key_callback);

	glfwMakeContextCurrent(window);
	gladLoadGLES2(glfwGetProcAddress);
	glfwSwapInterval(1);

	auto program = init_example();

	auto vertices = std::vector<glm::vec4>();
	vertices.reserve(mesh->getMaxTiles() * 4);
	dtNavMesh const* constMesh = mesh;
	int vId = 0;
	for (int i = 0; i < mesh->getMaxTiles(); ++i)
	{
		auto tile = constMesh->getTile(i);
		if (!tile->header)
			break;
		for (int j = 0; j < tile->header->detailVertCount; ++j)
		{
			vertices.push_back(glm::vec4(tile->detailVerts[3 * j], tile->detailVerts[3 * j + 1], tile->detailVerts[3 * j + 2], i));
			std::cout << "v " << vertices.rbegin()->x << " " << vertices.rbegin()->y << " " << vertices.rbegin()->z << std::endl;
			vId++;
		}
		std::cout << "f ";
		for (int j = 0; j < tile->header->detailVertCount; ++j)
			std::cout << (vId - j) << " ";
		std::cout << std::endl;
	}
	GLuint vertex_buffer;
	glGenBuffers(1, &vertex_buffer);
	auto error = glGetError();
	glBindBuffer(GL_ARRAY_BUFFER, vertex_buffer);
	glBufferData(GL_ARRAY_BUFFER, vertices.size() * sizeof(vertices[0]), vertices.data(), GL_STATIC_DRAW);

	auto vpos_location = glGetAttribLocation(program, "vPos");
	glEnableVertexAttribArray(vpos_location);
	glVertexAttribPointer(vpos_location, 4, GL_FLOAT, GL_FALSE, sizeof(vertices[0]), (void*)0);


	auto mvp_location = glGetUniformLocation(program, "MVP");
	while (!glfwWindowShouldClose(window))
	{
		auto time = (float)glfwGetTime();
		int width, height;
		glfwGetFramebufferSize(window, &width, &height);
		auto ratio = width / (float)height;

		glViewport(0, 0, width, height);
		glClear(GL_COLOR_BUFFER_BIT);

		auto m = glm::mat4(1.0f);
		// m = glm::rotate(m, time, glm::vec3(0.0f, 0.0f, 1.0f));
		auto v = glm::lookAt(glm::vec3(700, 700, 1500), (glm::vec3)vertices[0], glm::vec3(0, 0, 1));
		auto p = glm::ortho(-ratio, ratio, -1.f, 1.f, 0.1f, 2000.f);
		auto mvp = p * v * m;

		glUseProgram(program);
		glUniformMatrix4fv(mvp_location, 1, GL_FALSE, glm::value_ptr(mvp));
		glDrawArrays(GL_TRIANGLES, 0, 3 * vertices.size());

		glfwSwapBuffers(window);
		glfwPollEvents();
		// std::cout << "time " << time << std::endl;
	}

	std::cout << "Hello world!" << std::endl;
	return EXIT_SUCCESS;
}
