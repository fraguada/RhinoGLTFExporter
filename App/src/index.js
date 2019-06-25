const fs = require('fs')
const rhino3dm = require('rhino3dm')
const THREE = require('three')

// ---------------------
// https://gist.github.com/donmccurdy/9f094575c1f1a48a2ddda513898f6496
// needed to support THREE.GLTFExporter
const { Blob, FileReader } = require('vblob')
global.THREE = THREE
require('three/examples/js/exporters/GLTFExporter')
// ---------------------

let filepath = null
let ext = null
let scene = new THREE.Scene()
const exporter = new THREE.GLTFExporter()

// Parse arguments
process.argv.forEach(function (val, index, array) {
  console.log(val)
    if (index === 2) {
      filepath = val
    }
    if (index === 3){
        ext = val
    }
})

// handle no ext arg
if (ext === null) {
  // get extension from filepath
  ext = filepath.substring(filepath.lastIndexOf('.'))
  console.log(ext)
}

rhino3dm().then((rhino)=>{

  // patch global
  global.window = global
  global.Blob = Blob // working
  global.FileReader = FileReader
    
  if(ext === '.3dm'){

    //read 3dm file from disk
    let buffer = fs.readFileSync(filepath)
    let arr = new Uint8Array(buffer)
    let file3dm = rhino.File3dm.fromByteArray(arr)

    let objects = file3dm.objects()

    //2019.06.25 - File3dm.Materials not supported yet on rhino3dm.js
    
    //let materials = file3dm.materials
    //console.log(materials)

    for(var i=0; i<objects.count; i++) {

      let obj = objects.get(i)
      let geometry = obj.geometry()
      console.log(geometry)

      //let material = materials[obj.attributes.materialIndex]
     // console.log(material)

      let mat = new THREE.MeshPhysicalMaterial()

      //mat.color = material.diffuseColor

      let m = meshToThreejs(geometry, mat)
      scene.add(m)

    }

    exporter.parse( scene, function ( result ) {

      WriteFile(JSON.stringify( result, null, 2 ), filepath)

    })
  } 

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

