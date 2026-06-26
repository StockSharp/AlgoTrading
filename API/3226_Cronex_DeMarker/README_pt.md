# Cronex DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia Cronex DeMarker** reproduz o clássico consultor especialista Cronex que combina o oscilador DeMarker com uma pilha de dupla suavização. Primeiro, os valores do DeMarker são suavizados por uma média móvel simples rápida, então o resultado é suavizado mais uma vez por uma média mais lenta. A distância e a ordem relativa dessas duas linhas fornecem sinais de entrada estilo reversão.

A implementação MQL5 original permite alternâncias de direção de trade e funciona em períodos de tempo superiores. Este porto StockSharp mantém a mesma filosofia: reage quando a linha rápida cruza pela lenta e fecha imediatamente qualquer posição oposta. Como o sistema é contrário, um cruzamento abaixo da linha lenta abre uma posição comprada, enquanto um cruzamento acima abre uma vendida. Ambas as direções podem ser desabilitadas independentemente através de parâmetros, tornando a estratégia flexível para diferentes alocações de portfólio.

## Como funciona

1. Solicitar velas para o período selecionado (4H por padrão).
2. Calcular o oscilador DeMarker e suavizá-lo com uma SMA rápida (padrão 14 barras).
3. Aplicar uma segunda SMA (padrão 25 barras) sobre a linha rápida para obter a linha de sinal.
4. Quando a linha rápida estava acima da linha lenta na vela anterior e agora cai abaixo, a estratégia compra (reversão contrária). Qualquer posição vendida existente é achatada.
5. Quando a linha rápida estava abaixo da linha lenta na vela anterior e agora sobe acima, a estratégia vende e fecha qualquer posição comprada aberta.
6. O tamanho da posição é definido pela propriedade `Volume`; reversões usam a posição absoluta para inverter imediatamente.

Esta lógica permite ao especialista capturar movimentos de exaustão de curto prazo após fortes impulsos de momentum, tornando-o uma ferramenta de reversão à média que prefere mercados em range ou choppy.

## Parâmetros padrão

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `DeMarkerPeriod` | 25 | Número de barras usadas pelo oscilador DeMarker. |
| `FastPeriod` | 14 | Comprimento da primeira SMA de suavização aplicada aos valores do DeMarker. |
| `SlowPeriod` | 25 | Comprimento da SMA de sinal aplicada à linha rápida. |
| `CandleType` | 4 horas | Série de velas usada para cálculos de indicadores. |
| `EnableLongEntry` | true | Permitir entradas compradas contrárias quando a linha rápida cruza abaixo da linha lenta. |
| `EnableShortEntry` | true | Permitir entradas vendidas quando a linha rápida cruza acima da linha lenta. |
| `EnableLongExit` | true | Fechar posições compradas existentes quando condições baixistas aparecem. |
| `EnableShortExit` | true | Fechar posições vendidas existentes quando condições altistas aparecem. |

## Filtros e tags

- **Categoria**: Reversão à média, baseado em Oscilador
- **Direção**: Comprado & Vendido (configurável)
- **Indicadores**: DeMarker, Média Móvel Simples (dupla suavização)
- **Stops**: Nenhum (totalmente impulsado por sinal)
- **Período**: Swing trading (H4 por padrão, ajustável)
- **Complexidade**: Intermediário devido à cadeia de indicadores sequenciais
- **Perfil de risco**: Médio — entradas contrárias podem enfrentar tendências prolongadas
- **Automação**: Totalmente automatizado via API de alto nível do StockSharp

## Notas de uso

- A estratégia apenas processa velas finalizadas para evitar problemas de repintagem.
- Ordens de reversão reutilizam o tamanho absoluto da posição, garantindo achatamento imediato antes de entrar na nova direção.
- A saída do gráfico desenha as duas linhas suavizadas e marcadores de trade, ajudando na validação discrecional.
- Para portfólios que só permitem uma direção, desabilitar as entradas e saídas indesejadas através dos parâmetros fornecidos.
- Considerar adicionar controles de risco externos (stop-loss, saída trailing) ao implantar em ativos voláteis.
