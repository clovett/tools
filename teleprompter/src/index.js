import React from 'react';
import ReactDOM from 'react-dom';
import './index.css';
import InputFile from './inputfile';
import App from './App';
import * as serviceWorker from './serviceWorker';

function getSeconds(s) {
  var p = s.split(':');
  if (p.length === 3) {
    var hours = parseInt(p[0]);
    var minutes = parseInt(p[1]);
    var seconds = parseFloat(p[2]);
    return (hours * 3600) + (minutes * 60) + seconds;
  }
  return 0;
}

class CCEntry extends React.Component
{
  constructor(props, i, r, p)
  {
    super(props);

    var s = "";
    var e = "";
    var parts = r.split("-->");
    if (parts.length === 2) {
      s = parts[0];
      e = parts[1];
    }

    this.state = {index: i, range: r, start: getSeconds(s), end: getSeconds(e), prompt: p};

    this.handleMouseDown = this.handleMouseDown.bind(this);
  }

  handleMouseDown(e){
    e.target.contentEditable = true;
  }

  render() {
    return <div key={this.state.index}>
      <div className="srt-range">{this.state.range}</div>
      <xmp className="srt-prompt" onMouseDown={this.handleMouseDown} >{this.state.prompt}</xmp>
    </div>;
  }
}

class CCTable extends React.Component
{
  constructor(props) {
    super(props);
    this.state = {entries: []};
    this.loadFile = this.loadFile.bind(this)
  }

  parseSrt(text)
  {
    var entries = []
    var lines = text.split("\n");
    var pos = 0;
    var count = lines.length;
    while (pos < count)
    {
      var index = parseInt(lines[pos++]);
      if (isNaN(index) || pos >= count) {
        break;
      }
      var range = lines[pos++].trim();
      if (pos >= count) {
        break;
      }
      // consume the entire prompt up to next blank line.
      var prompt = lines[pos++].trim();
      while (pos < count && lines[pos].trim() !== "")
      {
        prompt += '\n' + lines[pos++].trim();
      }

      // skip blank lines.
      while (pos < count && lines[pos].trim() === ""){
        pos++;
      }

      entries.push(new CCEntry({}, index, range, prompt));
    }
    this.setState({entries: entries});

    if (window.onsrtloaded){
      // give this to index.html...
      window.onsrtloaded(entries);
    }

    return entries;
  }

  loadFile(e) {
    var file = e.target.files[0];
    window.file = file;
    var reader = new FileReader();
    var foo = this;
    reader.onload = function(e) {
      foo.parseSrt(e.target.result);
    };

    // Read in the srt file as a data URL.
    reader.readAsText(file);
  }

  render() {
    const rows = [];
    let i;
    for (i = 0; i < this.state.entries.length; i++)
    {
      rows.push(this.state.entries[i].render());
    }
    return <div>
      <div>
        SRT location:<br/>
        <InputFile id="SrtUrl" value="" className="fileprompt" accept=".srt, .txt"
               onChange={ this.loadFile } />
      </div>
      <div id="ccEntries" className="srt-table" >
        {rows}
      </div>
    </div>;
  }

  componentDidUpdate(){
    if (window.handle_resize){
      window.handle_resize();
    }
  }
}

ReactDOM.render(
  <CCTable></CCTable>,
  document.getElementById('ccTable')
);

// If you want your app to work offline and load faster, you can change
// unregister() to register() below. Note this comes with some pitfalls.
// Learn more about service workers: https://bit.ly/CRA-PWA
serviceWorker.unregister();
