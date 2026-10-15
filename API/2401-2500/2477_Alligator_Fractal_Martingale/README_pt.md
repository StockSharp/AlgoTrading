# Estratégia Alligator Fractal Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o Expert Advisor do MetaTrader "Alligator(barabashkakvn's edition)" para a API de alto nível do StockSharp. Combina o indicador Alligator de Bill Williams com confirmação de rompimento fractal, uma escada de média Martingale e trailing stops adaptativos. A lógica é projetada para execução no estilo hedge onde a primeira ordem é aberta a mercado e entradas adicionais são agendadas a distâncias predefinidas quando o preço se move contra a posição.

## Lógica de negociação

- **Expansão da boca do Alligator** – as médias móveis suavizadas dos lábios (verde), dentes (vermelho) e mandíbula (azul) são processadas no preço mediano. Um viés de compra é ativado quando os lábios sobem acima da mandíbula pelo menos `EntrySpread`, enquanto um viés de venda requer o alinhamento oposto. Quando o diferencial se contrai abaixo de `ExitSpread`, o viés respectivo é desativado.
- **Filtro fractal (opcional)** – as velas finalizadas são escaneadas em busca de fractais de Bill Williams. Um sinal de compra é aceito apenas se um fractal de alta dentro das últimas `FractalLookback` barras permanecer pelo menos `FractalBuffer` acima do fechamento. Os sinais de venda requerem um fractal de baixa abaixo do mercado. Desabilite o filtro através de `UseFractalFilter` para entrar apenas em sinais do Alligator.
- **Média Martingale** – após a ordem inicial de mercado, a estratégia pode pré-construir `MartingaleSteps` níveis de média espaçados por `MartingaleStepDistance`. Cada nível multiplica o volume anterior por `MartingaleMultiplier` (limitado por `MaxVolume`) e é executado assim que o preço toca o nível.
- **Gestão de saída com trailing** – cada posição comprada ou vendida preenchida recebe um stop-loss sintético e take-profit com base em `StopLossDistance` e `TakeProfitDistance`. Quando `EnableTrailing` está ativado, os stops são puxados para frente pelo menos `TrailingStep` à medida que o mercado se move a favor do trade.
- **Saídas pelo Alligator (opcional)** – quando `UseAlligatorExit` é verdadeiro, a posição é fechada assim que a boca do Alligator se fecha (o viés muda de ativo para inativo).

## Gerenciamento de risco e ordens

- A estratégia usa o parâmetro `Volume` para a primeira ordem de mercado. Cada nível martingale reutiliza o volume arredondado e o multiplica pelo fator configurado mantendo o resultado abaixo de `MaxVolume`.
- Stops e alvos são avaliados internamente em cada vela finalizada em vez de depender de ordens nativas da bolsa. Quando o intervalo da vela cruza o stop ou alvo sintético, a posição é fechada imediatamente.
- Posições opostas são fechadas antes de uma nova direção ser aberta para evitar exposição coberta dentro do StockSharp.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `Volume` | Tamanho base da ordem para a primeira entrada a mercado. |
| `JawLength`, `TeethLength`, `LipsLength` | Comprimento das médias móveis suavizadas que formam a mandíbula, dentes e lábios do Alligator. |
| `JawShift`, `TeethShift`, `LipsShift` | Deslocamento para frente (em barras) aplicado ao ler os buffers do Alligator. |
| `EntrySpread`, `ExitSpread` | Diferencial mínimo para habilitar trades e limiar de contração para desabilitá-los. |
| `UseAlligatorEntry`, `UseAlligatorExit` | Alternar entradas e saídas baseadas no Alligator. |
| `UseFractalFilter` | Habilitar ou desabilitar a camada de confirmação fractal. |
| `FractalLookback`, `FractalBuffer` | Janela de lookback e margem de segurança para fractais válidos. |
| `EnableMartingale`, `MartingaleSteps`, `MartingaleMultiplier`, `MartingaleStepDistance`, `MaxVolume` | Controlam a escada de média. |
| `StopLossDistance`, `TakeProfitDistance`, `EnableTrailing`, `TrailingStep` | Configuram o gerenciamento sintético de risco. |
| `AllowMultipleEntries` | Permitir entradas repetidas a mercado enquanto uma posição está aberta. |
| `ManualMode` | Quando verdadeiro, o algoritmo apenas gerencia trades abertos e não cria novos. |
| `CandleType` | Série de velas fonte para cálculos de indicadores. |

## Notas de uso

1. Certifique-se de que o instrumento selecionado suporta os passos de preço e volume configurados; a estratégia arredonda os valores usando `Security.MinPriceStep` e `Security.VolumeStep` quando disponíveis.
2. A escada martingale é simulada internamente. Se preferir usar ordens limite reais na bolsa, desabilite a função e gerencie o dimensionamento externamente.
3. Inicie a estratégia em um portfólio compatível com hedge. Embora o StockSharp agregue a posição líquida, a lógica original assume a capacidade de adicionar múltiplos elementos na mesma direção.
4. Revise as distâncias padrão baseadas em pips (`0.008` ≈ 80 pips para cotações FX de quatro dígitos) e ajuste-as ao instrumento sendo negociado.
