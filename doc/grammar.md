# Disclaimer: this grammar is wrong!
I use this grammar to clear my mind, but it's not accurate.

It's coded in a lighter version of EBNF where no , or ; is used for simplicity

For example: 
* Whitespace is ignored as it's handled by the tokenizer.
* Indentation is not represented, but it's fundamental for the language as it defines the blocks
```
source := topLevelSyntax+

topLevelSyntax := variableDeclaration | functionDeclaration

variableDeclaration := 'var' identifier ['=' expression] newline

functionDeclaration := 'fun' ['static'] identifier '(' ')' [':' type] newline {statement}

statement := declarationStatement | varOrCallChainMaybeAssignStatement
declarationStatement := ('let' | 'var') identifier [':' type] ['=' expression]
varOrCallChainMaybeAssignStatement := varOrCallChain ['=' expression]

varOrCallChain := varOrCall {'.' varOrCall}
varOrCall := identifier ['(' [expression {, expression}] ')']

expression := number | string | memberAccessExpression

type := identifier

identifier := ? identifier token ?
number := ? number token ?
string := ? string token ?
newline := ? newline token ?
```
