# EA Estratégia de identificação de gráfico OBJPROP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **EA estratégia de identificação de gráfico OBJPROP** recria o comportamento focado em gráfico do exemplo original MetaTrader 5 exibindo Donchian envelopes de canal em três períodos de tempo sincronizados. O gráfico principal hospeda o período de negociação enquanto dois painéis auxiliares visualizam o contexto H4 e Diário. Esta configuração reflete o Expert Advisor original, que empilhou vários gráficos e indicadores em um único espaço de trabalho para análise visual.

## Principais recursos

- **Visualização de vários períodos de tempo** – assina automaticamente velas primárias, H4 e diárias para o título selecionado.
- **Comprimento de canal Donchian unificado** – aplica o mesmo período de canal a cada período de tempo para manter os envelopes comparáveis.
- **Integração de gráficos de alto nível** – depende de áreas do gráfico StockSharp para renderizar séries de preços, canais Donchian e negociações executadas, reproduzindo o layout MQL sem manipulação de objetos de baixo nível.
- **Base extensível** – armazena os limites de canal mais recentes para cada período de tempo, facilitando a extensão da estratégia com lógica de ruptura ou confirmação no futuro.

## Parâmetros

| Parâmetro | Descrição | Categoria | Padrão |
|-----------|-------------|----------|---------|
| `ChannelLength` | Duração do canal Donchian usado em todos os períodos inscritos. | Indicadores | 22 |
| `PrimaryCandleType` | Período principal usado para negociação e como painel superior do gráfico. | Geral | Velas de 30 minutos |
| `H4CandleType` | Período auxiliar H4 exibido em painel secundário. | Geral | Velas de 4 horas |
| `DailyCandleType` | Período auxiliar diário exibido em painel terciário. | Geral | Velas de 1 dia |

Todos os parâmetros estão disponíveis por meio da IU de parâmetro StockSharp, suportam otimização e podem ser ajustados sem alterar o código.

## Lógica da estratégia

1. Inicializa três indicadores de canal Donchian com o mesmo parâmetro de comprimento.
2. Assina as séries de velas primárias, H4 e diárias selecionadas para o título atual.
3. Vincula cada assinatura ao seu respectivo indicador de canal usando o API de alto nível, garantindo que os valores do indicador sejam calculados de forma incremental.
4. Cria uma área principal do gráfico e até duas áreas auxiliares onde são desenhadas velas, canais e negociações da estratégia.
5. Armazena os limites superiores e inferiores mais recentes do canal para cada período de tempo, permitindo que regras de decisão personalizadas sejam adicionadas posteriormente.

A implementação atual é apenas de visualização e não envia pedidos. Isso reflete o código original MetaTrader, que se concentrava na composição de um painel de gráficos sem lógica de negociação automatizada.

## Notas de uso

- Certifique-se de que o título selecionado tenha dados históricos para cada período usado pela estratégia para preencher todas as áreas do gráfico.
- Você pode alterar qualquer um dos parâmetros do período de tempo para outros tipos de dados `TimeFrame` (por exemplo, 15 minutos ou velas semanais) se forem necessários painéis de contexto diferentes.
- Lógica comercial adicional pode ser colocada em camadas nos métodos de processamento (`ProcessPrimary`, `ProcessH4`, `ProcessDaily`) reagindo aos níveis de canal armazenados.

## Notas de conversão

- O exemplo MetaTrader criou gráficos filhos por meio de objetos `OBJ_CHART`; a versão StockSharp substitui isso por áreas de gráfico criadas pelo API de alto nível, que é melhor integrado à plataforma.
- O gerenciamento do indicador é realizado por meio de chamadas `BindEx` em vez da criação manual de identificadores, garantindo que os valores sejam sincronizados com as velas recebidas.
- As rotinas de exclusão de objetos não são necessárias porque StockSharp descarta automaticamente assinaturas e ligações de gráficos quando a estratégia é interrompida.
