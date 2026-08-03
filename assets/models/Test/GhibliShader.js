import {Color, Vector3} from "three";

export const GhibliShader ={
uniforms: {
    colorMap: {
        value: [
            new Color('#427062'),
            new Color('#33594e'),
            new Color('#234549'),
            new Color('#1e363f')
        ]
    },
    brightnessThreshold: {
        value: [0.9, 0.45, 0.001]
    },
    lightPosition: {
        value: new Vector3(15,15,15),
    },
},

        vertexShader: /* glsl */ `
        precision highp float;
        precision highp int;

        varying vec3 vNormal;
        varying vec3 vPosition; 

        void main() {
            vNormal = normal;
            vPosition = position;
            gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);

        }
        
        
        ` ,
        fragmentShader:/* glsl */  `

            precision highp float;
            precision highp int;
            uniform mat4 modelMatrix;

            uniform vec3 colorMap[4];
            uniform float brigthnessThresholds[3];
            uniform vec3 lightPosition;

            varying vec3 vNormal;
            varying vec3 vPosition; 

        void main() {
            vec3 worldPosition = ( modelMatrix * vec4( vPosition, 1.0 )).xyz;
            vec3 worldNormal = normalize( vec3(modelMatrix *  vec4( vNormal, 0.0)));
            vec3 lightVector = normalize( lightPosition * worldPosition );

            float brightness = dot(worldNormal, lightVector);

            vec4 final;

            if( brightness > brigthnessThresholds[0]) {
                final = vec4(colorMap[0], 1);
            }
            else if (brightness > brigthnessThresholds[1]) {
                final = vec4(colorMap[1], 1);
            }

            else if (brightness > brigthnessThresholds[2]) {
                final = vec4(colorMap[2], 1);
            }

            else {
                final = vec4(colorMap[1], 1);
            }
          
            gl_FragColor = vec4( final );
        }
        `,
};