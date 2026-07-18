# Estratégia de Rompimento BollTrade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o assessor especialista original **BollTrade** operando rompimentos das Bandas de Bollinger com um buffer de pips configurável e dimensionamento de posição opcional baseado no saldo. As ordens são abertas apenas em velas completadas e são gerenciadas com níveis fixos de stop-loss e take profit.

## Conceito

- Subscreve o período primário configurável e calcula um envelope das Bandas de Bollinger com o período e desvio especificados.
- Adiciona um deslocamento adicional (`Band Offset`) medido em unidades de pip acima da banda superior e abaixo da banda inferior para reduzir entradas prematuras.
- Abre uma posição **comprada** quando o fechamento da vela termina abaixo da banda inferior menos o deslocamento.
- Abre uma posição **vendida** quando o fechamento da vela termina acima da banda superior mais o deslocamento.
- Apenas uma posição pode estar ativa a qualquer momento. A estratégia aguarda o término da negociação atual antes de avaliar novas entradas.

## Gestão de Negociações

- Os níveis de stop-loss e take profit são definidos imediatamente após uma entrada. São expressos em múltiplos de pip e avaliados em cada vela completada. Se o preço tocar qualquer nível, a posição é fechada a mercado.
- Se `Scale Volume` estiver habilitado, o volume negociado cresce (ou diminui) com o saldo da conta. A linha de base de escala é o valor inicial do portfólio dividido pelo tamanho base do lote, imitando a implementação MQL original. O volume é limitado a 500 lotes para manter o risco sob controle, assim como no código fonte.
- O tamanho do pip é derivado do passo de preço do instrumento. Para passos muito pequenos (símbolos no estilo forex), o código multiplica o passo por 10 para converter passos de pip fracionários em pips padrão, correspondendo ao comportamento da versão MetaTrader.

## Parâmetros

| Nome | Descrição | Valor padrão |
| ---- | --------- | ------------ |
| `Candle Type` | Período usado para velas de sinal. | Período de 15 minutos |
| `Bollinger Period` | Número de barras no cálculo das Bandas de Bollinger. | 4 |
| `Bollinger Deviation` | Multiplicador de largura para as Bandas de Bollinger. | 2 |
| `Band Offset` | Deslocamento adicional em pip adicionado fora de ambas as bandas antes de acionar sinais. | 3 |
| `Take Profit (pips)` | Distância ao alvo de lucro em unidades de pip. | 3 |
| `Stop Loss (pips)` | Distância ao stop de proteção em unidades de pip. | 20 |
| `Base Volume` | Volume padrão em lotes quando o dimensionamento está desabilitado. | 1 |
| `Scale Volume` | Quando habilitado, escala o tamanho da posição com o saldo da conta. | Habilitado |

## Notas de uso

- Funciona melhor em símbolos forex ou CFD onde deslocamentos baseados em pip fornecem níveis de rompimento claros, mas também pode ser executado em futuros ou ações desde que seu `PriceStep` esteja configurado.
- A estratégia processa apenas velas terminadas, portanto picos intrabarra que revertem antes do fechamento da barra não acionarão entradas.
- Como as saídas são tratadas com stops e alvos fixos, certifique-se de que essas distâncias sejam apropriadas para o período selecionado e a volatilidade do instrumento.
- O EA original dependia de stops do lado do broker. Este port monitora os extremos das velas para emular o mesmo comportamento de proteção dentro do StockSharp.

## Arquivos

- `CS/BollTradeStrategy.cs` – implementação em C# da estratégia.
- `README.md` – documentação em inglês (este arquivo).
- `README_ru.md` – documentação em russo.
- `README_zh.md` – documentação em chinês.
