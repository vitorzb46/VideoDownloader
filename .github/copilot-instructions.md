# Extensões e Skills Ativas (.copilot)

Este repositório está integrado às skills do sistema local. Sempre que um dos comandos abaixo for invocado no chat, carregue as diretrizes da respectiva subpasta localizada no diretório global do Copilot (`C:\Users\Vitor\.copilot\skills...`):

- **`/ponytail`** ou **`ponytail`**: Ativa a lógica central YAGNI baseada em código mínimo.
- **`/ponytail-review`**: Carrega as regras da pasta `ponytail-review` para analisar o arquivo atual em busca de excesso de engenharia.
- **`/ponytail-audit`**: Carrega as regras da pasta `ponytail-audit` para varrer redundâncias estruturais no escopo de `#solution`.
- **`/ponytail-debt`**: Carrega as regras da pasta `ponytail-debt` para identificar acúmulo de complexidade desnecessária.
- **`/ponytail-gain`**: Carrega as regras de ganho de performance minimalista da pasta `ponytail-gain`.
- **`/ponytail-help`**: Consulta o manual da pasta `ponytail-help`.
- **`/debug`** ou **`systematic-debugging`**: Ativa a skill de investigação contida na pasta `systematic-debugging`. Obriga o assistente a aplicar a "Lei de Ferro" (proibido propor correções sem antes isolar e provar a causa raiz do erro).

*Ação:* Ao receber qualquer um destes prefixos, mescle as instruções da pasta global correspondente com o contexto atual da solução do Visual Studio.


\# Diretrizes do Projeto: Contexto de Arquitetura Graphify



\- Sempre que o usuário fizer perguntas sobre estrutura, dependências ou fluxo de classes, consulte as relações mapeadas do projeto.

\- O grafo de arquitetura estrutural deste repositório está gerado e disponível localmente no caminho: `./graphify-out/graph.json`.

\- Utilize o conhecimento de conexões deste grafo para responder de forma concisa e contextualizada.



