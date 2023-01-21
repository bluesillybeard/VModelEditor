#version 330 core

out vec4 outputColor;

in vec2 texCoord;

uniform sampler2D tex;

void main()
{
    outputColor = texture(tex, texCoord);
    if(outputColor.a < .5)discard;
}