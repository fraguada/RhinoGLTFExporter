const fs = require('fs')
const rhino3dm = require('rhino3dm')
const THREE = require('three')

// ---------------------
// https://gist.github.com/donmccurdy/9f094575c1f1a48a2ddda513898f6496
// needed to support THREE.GLTFExporter
const { Blob, FileReader } = require('vblob')
global.THREE = THREE
global.Blob = Blob
global.FileReader = FileReader;

require('three/examples/js/exporters/GLTFExporter')
// ---------------------

let file3dmpath = null
let ext = null
let scene = new THREE.Scene()
const exporter = new THREE.GLTFExporter()

process.argv.forEach(function (val, index, array) {
    if (index === 2) {
        file3dmpath = val
    }
    if (index === 3){
        ext = val
    }
})

rhino3dm().then((rhino)=>{

    global.window = global

    let buffer = fs.readFileSync(file3dmpath)
    let arr = new Uint8Array(buffer)
    let file3dm = rhino.File3dm.fromByteArray(arr)
    let objects = file3dm.objects()
    let materials = file3dm.materials

    for(var i=0; i<objects.count; i++) {

        let obj = objects.get(i)
        let geometry = obj.geometry()
        //let material = materials[obj.attributes.materialIndex]
        console.log(geometry)

        let mat = new THREE.MeshPhysicalMaterial()
        //mat.color = material.diffuseColor

        let m = meshToThreejs(geometry, mat)
        scene.add(m)

      }

    exporter.parse( scene, function ( result ) {

        WriteFile(JSON.stringify( result, null, 2 ), file3dmpath)

    })

})

function WriteFile (txt, file) {
    fs.writeFile(file + ".glTF", txt, (err) => {
        if (err) throw err
        //console.log('The file has been saved! ' + file + ".glTF")
    })
}

function meshToThreejs(mesh, material) {
    var geometry = new THREE.BufferGeometry();
    var vertices = mesh.vertices();
    var vertexbuffer = new Float32Array(3 * vertices.count);
    for( var i=0; i<vertices.count; i++) {
      pt = vertices.get(i);
      vertexbuffer[i*3] = pt[0];
      vertexbuffer[i*3+1] = pt[1];
      vertexbuffer[i*3+2] = pt[2];
    }
    // itemSize = 3 because there are 3 values (components) per vertex
    geometry.addAttribute( 'position', new THREE.BufferAttribute( vertexbuffer, 3 ) );
  
    indices = [];
    var faces = mesh.faces();
    for( var i=0; i<faces.count; i++) {
      face = faces.get(i);
      indices.push(face[0], face[1], face[2]);
      if( face[2] != face[3] ) {
        indices.push(face[2], face[3], face[0]);
      }
    }
    geometry.setIndex(indices);
  
    var normals = mesh.normals();
    var normalBuffer = new Float32Array(3*normals.count);
    for( var i=0; i<normals.count; i++) {
      pt = normals.get(i);
      normalBuffer[i*3] = pt[0];
      normalBuffer[i*3+1] = pt[1];
      normalBuffer[i*3+2] = pt[1];
    }
    geometry.addAttribute( 'normal', new THREE.BufferAttribute( normalBuffer, 3 ) );
    return new THREE.Mesh( geometry, material );
  }

