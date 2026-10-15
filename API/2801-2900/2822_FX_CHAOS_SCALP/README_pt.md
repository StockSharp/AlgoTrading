# Estratégia de Scalping FX-CHAOS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A estratégia de scalping FX-CHAOS replica o expert advisor MT5 que combina o Awesome Oscillator com níveis ZigZag baseados em fractais em múltiplos períodos. A versão StockSharp inscreve-se em velas horárias para execução de operações e velas diárias para um filtro de período superior. Rastreadores internos reconstroem a lógica do "ZigZag on Fractals" detectando padrões fractais de cinco velas e conectando-os em pontos de oscilação alternados.

## Fluxo de Trading
1. **Coleta de dados**
   - Velas horárias impulsionam as entradas e o gerenciamento de risco.
   - Velas diárias alimentam o filtro ZigZag de período superior.
   - Um Awesome Oscillator (5, 34) é calculado no feed horário.
2. **Rastreamento do ZigZag fractal**
   - Cada vela concluída é inserida em uma janela deslizante de cinco elementos.
   - Quando a barra central forma um fractal ascendente/descendente, o último valor de oscilação é atualizado; oscilações consecutivas na mesma direção são substituídas apenas por valores mais extremos.
3. **Detecção de sinais no fechamento horário**
   - Um sinal de compra aparece quando a vela abre abaixo da máxima anterior, fecha acima dela, permanece abaixo da última oscilação ZigZag horária, fica acima do nível ZigZag diário mais recente e o Awesome Oscillator é negativo.
   - Um sinal de venda espelha a lógica usando a mínima anterior e a polaridade oposta do oscilador.
4. **Execução de ordens**
   - Posições opostas existentes são liquidadas antes de um novo entrada ser colocada com o volume configurado.
   - O preço de entrada é armazenado para o gerenciamento subsequente de stop loss e take profit.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `Volume` | Volume de trading em lotes. Aplicado a cada ordem de mercado. |
| `Stop Loss (pts)` | Distância em pontos para o stop protetor. O valor é multiplicado pelo passo de preço do instrumento. Defina como `0` para desabilitar. |
| `Take Profit (pts)` | Distância em pontos para o objetivo de lucro. Convertido com o passo de preço da mesma forma. Defina como `0` para desabilitar. |
| `Trading Candle` | Período principal usado para entradas (padrão 1 hora). |
| `Daily Candle` | Período superior usado para o filtro ZigZag (padrão 1 dia). |

## Gerenciamento de Risco
- Em cada vela horária concluída, a estratégia verifica se o preço tocou o nível de stop loss ou take profit derivado do preço de entrada armazenado.
- Uma ordem protetora preenchida fecha a posição imediatamente e reinicia o sinalizador de preço de entrada, evitando uma re-entrada no mesmo ciclo de vela.
- As posições também são liquidadas quando um novo sinal na direção oposta aparece.

## Notas de Implementação
- A lógica ZigZag personalizada evita buffers diretos de indicadores e segue as diretrizes do repositório trabalhando em assinaturas de velas com estado local mínimo.
- Os valores ZigZag permanecem `null` até que velas suficientes sejam processadas (duas barras em cada lado de um fractal potencial). O trading é suspenso até que ambos os rastreadores horários e diários produzam oscilações válidas.
- O Awesome Oscillator é solicitado via `BindEx`, garantindo que a estratégia use apenas valores finais do indicador quando todas as entradas estão prontas.
- As distâncias de preço são dimensionadas por `Security.PriceStep`. Se o instrumento não tiver um passo, a estratégia recorre a um multiplicador de um ponto.

## Arquivos
- `CS/FxChaosScalpStrategy.cs` – implementação da estratégia com o rastreador ZigZag, filtro Awesome Oscillator e lógica de ordens.
- `README_zh.md` – documentação em chinês simplificado.
- `README_ru.md` – documentação em russo.
