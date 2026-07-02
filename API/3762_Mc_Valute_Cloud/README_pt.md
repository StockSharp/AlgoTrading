# Estratégia de nuvem da Mc Value
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta pasta contém a porta StockSharp do consultor especialista MetaTrader "Mc_valute". O robô original combinou um curto
média móvel exponencial (EMA) com três médias móveis suavizadas, um filtro de nuvem Ichimoku e múltiplas instâncias MACD enquanto
escalando a tendência. A implementação StockSharp mantém a pilha principal de confirmação de tendências, mas simplifica o gerenciamento de posição
a uma única exposição em cada direção para que a lógica se encaixe naturalmente no API de alto nível.

## Lógica de negociação

1. **Filtro de preço EMA** – o `FilterMaLength` EMA deve ficar acima (para posições compradas) ou abaixo (para posições vendidas) dos dois movimentos suavizados
médias (`BlueMaLength` e `LimeMaLength`). As médias suavizadas emulam as linhas “azul” e “limão” do modelo MT4.
2. **Ichimoku confirmação da nuvem** – o EMA também precisa estar fora da nuvem. Negociações longas requerem o filtro EMA acima de ambos
Senkou se estende enquanto as negociações curtas exigem que ele permaneça abaixo do fundo da nuvem.
3. **MACD verificação de impulso** – a linha MACD principal deve estar acima de sua linha de sinal para entradas longas e abaixo dela para entradas curtas.
Apenas o primeiro MACD conjunto do EA original é mantido porque as cópias restantes foram desativadas na versão final do MQL.
4. **Gerenciamento de posição única** – sempre que um novo sinal aparece, a estratégia compensa qualquer posição oposta existente e abre uma
nova negociação com o `Volume` configurado. As ordens de proteção são atualizadas imediatamente após o envio da ordem de mercado.
5. **Avaliação vela por vela** – todos os indicadores operam no prazo definido por `CandleType`. As decisões de negociação são tomadas
apenas em velas finalizadas para espelhar o manipulador MT4 `start()` que processou barras fechadas.

## Gestão de risco

- `TakeProfit` e `StopLoss` são medidos em faixas de preço. Após cada entrada, o ajudante `SetTakeProfit` e `SetStopLoss`
funções são chamadas usando o tamanho de posição resultante esperado, que reflete o comportamento do MT4 onde as paradas foram aplicadas por
bilhete.
- O consultor especialista original fez uma pirâmide de até três pedidos adicionais usando a distância `Step`. A porta StockSharp mantém um
posição única para permanecer entre os ajudantes de pedidos de alto nível. Os usuários que precisam de escalonamento podem aumentar `Volume` ou clonar o
estratégia em vários portfólios.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `Volume` | Tamanho base da negociação usado pelas chamadas `BuyMarket`/`SellMarket` de alto nível. |
| `CandleType` | Série primária de velas que orienta os indicadores e a lógica comercial. |
| `FilterMaLength` | Comprimento do filtro de tendência EMA. |
| `BlueMaLength`, `LimeMaLength` | Comprimentos das duas médias móveis suavizadas atuando como banda direcional. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | EMA comprimentos para a confirmação MACD. |
| `TenkanLength`, `KijunLength`, `SenkouLength` | Ichimoku Configurações Kinko Hyo para o filtro de nuvem. |
| `TakeProfit`, `StopLoss` | Distâncias de proteção expressas em faixas de preço. |

## Notas de uso

1. **Mudanças de indicador** – MetaTrader permitiu parâmetros de "mudança" diferentes de zero ao construir as médias móveis suavizadas. StockSharp
os indicadores funcionam na barra atual, portanto a porta ignora essas mudanças enquanto mantém os períodos originais.
2. **MACD variantes** – o código-fonte declarou três blocos MACD, mas apenas o primeiro participou de sinais ao vivo. O porto
segue esse comportamento; filtros MACD adicionais podem ser reativados duplicando as ligações do indicador.
3. **Negociações de escalonamento** – o robô MT4 enviou até três pedidos médios separados por `Step` pontos. Este comportamento está documentado
mas omitido intencionalmente porque as estratégias de alto nível operam com uma única posição agregada.
4. **Bloco de proteção** – `StartProtection()` é invocado uma vez durante a inicialização para que a infraestrutura integrada supervisione a parada
e direcionar pedidos mesmo após reconexões.

## Arquivos

- `CS/McValuteCloudStrategy.cs` – Implementação C# usando a estratégia de alto nível API com vinculações de indicadores e detalhes
comentários.
- `README.md` – Documentação em inglês (este arquivo).
- `README_zh.md` – Tradução simplificada para chinês.
- `README_ru.md` – Tradução russa.
