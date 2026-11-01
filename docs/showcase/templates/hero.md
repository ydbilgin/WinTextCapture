<p align="center"><img src="{{icon}}" alt="{{name}} icon" width="88"></p>
<h1 align="center">{{name}}</h1>
<p align="center">
  <strong>{{tagline}}</strong><br>
  {{description}}
</p>
<p align="center">
{{#badges}}
  {{#link}}<a href="{{link}}">{{/link}}<img alt="{{alt}}" src="{{image}}">{{#link}}</a>{{/link}}
{{/badges}}
</p>
{{#hero}}
<p align="center">
  {{#hotkey}}<kbd>{{key}}</kbd>{{^last}} + {{/last}}{{/hotkey}} &nbsp;→&nbsp; drag a box &nbsp;→&nbsp; <kbd>Ctrl</kbd> + <kbd>V</kbd>
</p>
<p align="center">
  <img src="{{media}}" alt="{{alt}}" width="800"><br>
  <sub>{{caption}}{{#caption_link}} <a href="{{href}}">{{text}}</a>.{{/caption_link}}</sub>
</p>
<p align="center">
{{#nav}}
  {{#first}}<a href="{{link}}"><strong>{{label}}</strong></a>{{/first}}{{^first}}<a href="{{link}}">{{label}}</a>{{/first}}{{^last}} &nbsp;·&nbsp;{{/last}}
{{/nav}}
</p>
{{/hero}}
