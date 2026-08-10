![Logotipo da StockSharp](logo.png)

# Exemplos de trading algorítmico com StockSharp

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este é o repositório oficial da StockSharp com exemplos de trading algorítmico. Ele reúne um amplo catálogo organizado de estratégias para a API, exemplos visuais do Strategy Designer, material educativo e verificações automáticas que mantêm os exemplos compiláveis.

O repositório destina-se a estudo, pesquisa, prototipagem e testes de regressão. As estratégias ilustram ideias de trading e o uso das APIs da StockSharp; elas não são recomendações de investimento prontas para uso.

## Por onde começar

| Objetivo | Localização |
|---|---|
| Explorar estratégias por ideia de trading | [Catálogo de estratégias da API](API/README_pt.md) |
| Estudar implementações em C# e Python | [`API`](API/) |
| Explorar esquemas visuais e exemplos do Designer | [`Designer`](Designer/) |
| Compilar e testar uma estratégia em C# | [`Backtester`](Backtester/) |
| Examinar a estrutura de testes automáticos | [`Tests`](Tests/) |

## Conteúdo do repositório

### Catálogo de estratégias da API

O diretório [`API`](API/) contém desde abordagens conhecidas — cruzamentos de médias móveis, rompimentos, momentum, volatilidade e reversão à média — até pairs trading, arbitragem, market making, métodos de portfólio, modelos de fluxo de ordens, experimentos de aprendizado de máquina e muitas variações especializadas.

O catálogo agrupa as estratégias pela principal ideia de trading, enquanto o sistema de arquivos usa diretórios com intervalos numéricos para que o GitHub possa exibir a grande coleção com eficiência. Um exemplo típico tem a seguinte estrutura:

```text
API/0001-0100/0001_MA_CrossOver/
├── CS/
│   ├── MaCrossoverStrategy.cs
│   └── logo.svg
├── PY/
│   ├── ma_crossover_strategy.py
│   └── logo.svg
├── README.md
├── README_ru.md
├── README_zh.md
├── README_es.md
├── README_de.md
├── README_pt.md
└── README_ja.md
```

Cada exemplo da API implementa a mesma ideia de estratégia em C# e Python. A documentação em sete idiomas explica o conceito, os parâmetros, a lógica dos sinais e os riscos. Os logotipos SVG transparentes identificam a estratégia e a linguagem de implementação e permanecem legíveis nos temas claro e escuro.

### Exemplos do Strategy Designer

O diretório [`Designer`](Designer/) contém esquemas visuais, tipos de estratégia reutilizáveis e exemplos educativos para o [StockSharp Strategy Designer](https://doc.stocksharp.com/en/topics/designer.html). Eles são úteis quando se prefere montar e analisar uma estratégia graficamente em vez de começar diretamente pelo código-fonte.

### Ferramentas de compilação e teste

O repositório inclui dois pequenos projetos .NET:

- [`Backtester`](Backtester/) compila dinamicamente uma estratégia C# selecionada e a executa com os dados históricos de exemplo incluídos.
- [`Tests`](Tests/) compila os exemplos da API e os verifica no ambiente de emulação histórica da StockSharp.

O projeto de testes usa um gerador de código-fonte, portanto estratégias comuns não precisam de métodos de teste escritos manualmente. Cada teste gerado executa uma estratégia com dados de mercado de exemplo, verifica a criação de ordens e negócios e testa a clonagem e a serialização das configurações. Estratégias que exigem vários instrumentos ou configuração especial possuem implementações explícitas no projeto de testes.

Antes da compilação .NET, [`Tools/validate_api_structure.py`](Tools/validate_api_structure.py) executa verificações estruturais rápidas: localização correta nos diretórios numerados, paridade entre C# e Python, traduções obrigatórias, presença dos arquivos-fonte e ausência de afirmações desatualizadas sobre uma versão de linguagem indisponível.

## Pré-requisitos

Para compilar a solução completa localmente, instale:

- o SDK do .NET 10;
- Python 3 para o validador de estrutura;
- um checkout do repositório da plataforma StockSharp ao lado deste repositório.

As referências de projeto esperam a seguinte estrutura de diretórios:

```text
<workspace>/
├── AlgoTrading/
└── StockSharp (GitHub)/
```

Clone este repositório como `AlgoTrading` e o [repositório da plataforma StockSharp](https://github.com/StockSharp/StockSharp) como `StockSharp (GitHub)` sob o mesmo diretório pai.

## Validar, compilar e testar

Execute primeiro as verificações rápidas do repositório:

```bash
python Tools/validate_api_structure.py
```

Depois, compile e teste a solução com a mesma configuração usada pela CI:

```bash
dotnet build AlgoTrading.slnx --configuration Release
dotnet test AlgoTrading.slnx --no-build --configuration Release
```

Para executar apenas um teste de estratégia gerado, filtre pelo nome da pasta da estratégia em PascalCase. Por exemplo:

```bash
dotnet test Tests/Tests.csproj --no-build --configuration Release \
  --filter "FullyQualifiedName~MaCrossover"
```

Para compilar e testar diretamente um exemplo em C#:

```bash
dotnet run --project Backtester/Backtester.csproj -- \
  API/0001-0100/0001_MA_CrossOver/CS/MaCrossoverStrategy.cs
```

## Como usar os exemplos

Escolha uma estratégia no [catálogo](API/README_pt.md), leia suas premissas e parâmetros e compare as implementações em C# e Python. Trate cada exemplo como um ponto de partida: selecione dados de mercado, comissões, slippage, latência, dimensionamento de posição e limites de risco adequados antes de avaliar a ideia.

Para desenvolvimento visual, instale o [Strategy Designer](https://stocksharp.com/en/store/strategy-designer/), abra a [Strategy Gallery](https://doc.stocksharp.com/en/topics/designer/strategy_gallery.html) e use os esquemas em [`Designer`](Designer/) como material de estudo ou protótipos.

Sempre valide uma estratégia modificada com dados fora da amostra e em simulação antes de considerar a execução ao vivo. Um backtest demonstra o comportamento em um conjunto de dados específico; ele não comprova rentabilidade futura.

## Como contribuir

São bem-vindas contribuições que melhorem a correção, clareza, cobertura ou valor educativo. Ao adicionar ou alterar uma estratégia da API:

1. Mantenha a estratégia no diretório do intervalo numérico correspondente.
2. Mantenha as implementações em C# e Python.
3. Mantenha os sete README localizados alinhados aos parâmetros e ao comportamento reais.
4. Adicione ou atualize os logotipos SVG transparentes de cada linguagem quando a identidade visual da estratégia mudar.
5. Execute o validador de estrutura, a compilação Release e os testes relevantes antes de abrir um pull request.

Estratégias comuns são descobertas automaticamente pelo gerador de testes. Adicione uma implementação manual somente quando o exemplo precisar de instrumentos, portfólios, dados de mercado ou outra configuração que a estrutura padrão não possa fornecer.

## Recursos

- [Site da StockSharp](https://stocksharp.com/)
- [Documentação](https://doc.stocksharp.com/en/)
- [Strategy Designer](https://stocksharp.com/en/store/strategy-designer/)
- [Chat da comunidade](https://stocksharp.com/en/chat/)
- [Rastreador de problemas](https://github.com/StockSharp/AlgoTrading/issues)

## Licença e aviso de risco

Consulte [LICENSE](LICENSE) e [NOTICE](NOTICE) para conhecer os termos aplicáveis a este repositório.

O trading algorítmico envolve riscos substanciais. Os exemplos são fornecidos para fins educacionais e técnicos, sem qualquer garantia de desempenho. Antes de usar dinheiro real, você é responsável por revisar o código, validar as premissas e aplicar controles adequados de risco operacional e financeiro.
