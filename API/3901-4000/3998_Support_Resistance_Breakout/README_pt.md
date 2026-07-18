# Estratégia de ruptura de suporte e resistência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o especialista "SupportResistTrade" MetaTrader combinando um rompimento de suporte e resistência recentes com um filtro de tendência de longo prazo EMA. As negociações são abertas somente quando o preço ultrapassa o limite do canal Donchian **e** a vela abre no mesmo lado de uma média móvel exponencial longa. O risco é gerenciado por meio de paradas de proteção imediatas e uma rotina de rastreamento de três etapas que garante lucros em +10, +20 e +30 pontos.

## Dados e Indicadores
- **Feed principal:** assinatura de vela única (período padrão de 1 minuto, configurável por meio de `CandleType`).
- **Suporte/Resistência:** `DonchianChannels` com comprimento `RangeLength` (padrão 55) para rastrear o máximo mais alto e o mínimo mais baixo do intervalo recente.
- **Filtro de tendência:** `ExponentialMovingAverage` sobre aberturas de vela com período `EmaPeriod` (padrão 500). Somente posições longas com preço acima de EMA e posições curtas com preço abaixo de EMA são aceitas.

## Lógica de negociação
1. **Análise de mercado:** em cada vela finalizada o intervalo Donchian e EMA são atualizados. A banda superior é tratada como resistência e a banda inferior como suporte.
2. **Condições de entrada:**
   - **Longa:** a vela fecha acima da resistência *e* sua abertura foi acima de EMA. Qualquer venda existente é fechada e uma ordem de mercado comprada é enviada.
   - **Venda:** a vela fecha abaixo do suporte *e* sua abertura ficou abaixo de EMA. Qualquer posição longa existente é fechada e uma ordem de mercado curta é enviada.
3. **Parada inicial:** após um preenchimento, uma ordem de stop é colocada no último suporte (para posições longas) ou resistência (para posições vendidas), refletindo o comportamento de stop-loss MQL.
4. **Lógica de saída:**
   - Quando a negociação é lucrativa e o fechamento retorna além da banda de suporte/resistência atualizada, a posição é fechada no mercado, correspondendo à condição de saída manual do EA.
   - A parada protetora permanece ativa para que reversões repentinas sejam detectadas automaticamente.

## Parada final
Um mecanismo de rastreamento testado reproduz as três chamadas `OrderModify` do EA:
| Limite de lucro (pontos) | Nova distância de parada (pontos) | Descrição |
| --- | --- | --- |
| `>= 20` | `10` | Parada longa salta para entrada + 10 pontos (parada curta para entrada − 10). |
| `>= 40` | `20` | Stop move para entrada +/− 20 pontos. |
| `>= 60` | `30` | A etapa final garante 30 pontos de lucro. |
A lógica nunca afrouxa o stop: para posições compradas, o stop só pode se mover para cima, enquanto para posições vendidas, ele só pode se mover para baixo.

## Gestão de risco
- Todas as paradas são implementadas como ordens de parada nativas (`SellStop`/`BuyStop`) para que o corretor lide com a execução mesmo se a estratégia for brevemente desconectada.
- A estratégia funciona com base na posição líquida; cada novo sinal fecha na direção oposta antes de estabelecer uma nova negociação.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `RangeLength` | `55` | Número de velas usadas para calcular suporte (baixo) e resistência (alta). |
| `EmaPeriod` | `500` | Período do filtro de tendência EMA aplicado às aberturas de velas. |
| `CandleType` | `1 Minute` | Série de velas usada para todos os cálculos (pode ser alterada para qualquer outro período de tempo). |

## Notas
- O código é escrito no StockSharp API de alto nível apenas com vinculação de indicadores e assinaturas de velas.
- Nenhuma porta Python é fornecida. A pasta `CS` contém a única implementação.
