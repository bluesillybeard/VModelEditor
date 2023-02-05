#version 330

out vec4 outputColor;

in vec2 texCoord;
in vec3 normal;

uniform sampler2D tex;

void main()
{
    outputColor = texture(tex, texCoord);
    if(outputColor.a < .5)discard;
}