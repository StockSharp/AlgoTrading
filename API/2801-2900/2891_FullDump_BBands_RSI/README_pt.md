# Estratégia FullDump BB RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um sistema de múltiplos passos baseado em Bollinger Bands e RSI, convertido do especialista MT5 "FullDump". A estratégia aguarda o esgotamento do momentum, confirma um viés de reversão à média com Bollinger Bands e só opera quando o preço se realinha com a banda média. O gerenciamento de operações espelha o EA original com offsets fixos de stop-loss/alvo e um ajuste de break-even quando o preço retorna à banda oposta.

## Visão Geral

- **Mercados**: Qualquer instrumento líquido que suporte Bollinger Bands e RSI.
- **Período**: Tipo de vela configurável (padrão 15 minutos).
- **Direção**: Comprado/Vendido.
- **Tipo de Ordem**: Ordens a mercado com níveis de proteção predefinidos.
- **Conceito**: Desvanecimento de extremos de curto prazo dentro do envelope de Bollinger enquanto o preço reverte para a banda média.

## Lógica de Trading

1. **Varredura RSI (Passo 1)**
   - A condição de compra requer pelo menos uma leitura RSI abaixo de 30 dentro da janela recente.
   - A condição de venda requer pelo menos uma leitura RSI acima de 70 dentro do mesmo lookback.
2. **Violação de banda (Passo 2)**
   - Compra: o fechamento atual deve estar abaixo ou igual a qualquer um dos valores recentes da banda inferior.
   - Venda: o fechamento atual deve estar acima ou igual a qualquer um dos valores recentes da banda superior.
3. **Alinhamento com banda média (Passo 3)**
   - Operações compradas só são acionadas quando o preço fecha de volta acima da linha média de Bollinger.
   - Operações vendidas requerem que o fechamento esteja abaixo da linha média.
4. **Execução de entrada**
   - Quando todas as condições correspondem e não há posição aberta nessa direção, uma ordem a mercado é enviada pelo volume configurado.

## Gestão de Risco

- **Stop-loss**: Colocado abaixo (comprado) ou acima (vendido) da mínima/máxima extrema da janela de lookback menos/mais o offset de recuo configurado.
- **Take-profit**: Colocado na banda de Bollinger oposta atual mais o mesmo offset de recuo.
- **Regra break-even**: Quando o preço toca a banda oposta, o stop-loss é movido para o preço de entrada para garantir a posição.
- **Saída de posição**: As posições fecham quando o preço ultrapassa os níveis de stop-loss ou take-profit; sinais opostos aplainam a posição atual antes de reverter a direção.

## Parâmetros

| Nome | Descrição | Padrão | Notas |
| --- | --- | --- | --- |
| `BandsPeriod` | Comprimento do cálculo de Bollinger Bands. | 20 | Otimizável (10 → 40 passo 1). |
| `RsiPeriod` | Comprimento de média para o RSI. | 14 | Otimizável (7 → 21 passo 1). |
| `Depth` | Número de velas recentes inspecionadas para condições. | 6 | Otimizável (3 → 12 passo 1). |
| `IndentInPoints` | Offset em passos de preço adicionado ao stop-loss e take-profit. | 10 | Otimizável (5 → 30 passo 5). |
| `OrderVolume` | Tamanho da ordem em lotes. | 1 | Usado tanto para entradas quanto saídas. |
| `CandleType` | Período das velas de entrada. | Velas de 15 minutos | Alterar para adaptar o horizonte da estratégia. |

## Filtros e Tags

- **Categoria**: Reversão à média, bandas de volatilidade.
- **Indicadores**: Bollinger Bands, Relative Strength Index.
- **Stops**: Stop fixo, alvo fixo, ajuste break-even.
- **Complexidade**: Intermediário (lógica multi-condição com gestão com estado).
- **Nível de Automação**: Entradas e saídas totalmente automatizadas.
- **Melhor Uso**: Fases de intervalo limitado onde os extremos de Bollinger frequentemente revertem para a mediana.

## Notas

- O offset de recuo é escalado pelo passo de preço do instrumento para corresponder à lógica baseada em pips do EA original.
- O algoritmo mantém filas dos valores recentes do indicador para replicar exatamente as verificações de profundidade do MT5.
- Garantir que o instrumento forneça velas históricas suficientes para inicializar tanto RSI quanto Bollinger Bands antes do trading ao vivo.
