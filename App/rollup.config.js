// ref https://risanb.com/posts/bundling-your-javascript-library-with-rollup/
import commonjs from 'rollup-plugin-commonjs';
import resolve from 'rollup-plugin-node-resolve';

export default {
    input: 'src/index.js',
    output: {
      file: 'dist/index.js',
      format: 'cjs'
    },
    plugins: [
      resolve(),
      commonjs({
        include: 'node_modules/**'
      })
    ]
  };