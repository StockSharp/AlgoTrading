# Estratégia de Rompimento GBP às 9 AM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Rompimento GBP às 9 AM** replica o antigo assessor especializado "GBP9AM" do MetaTrader no StockSharp. O sistema prepara um straddle em torno da abertura de Londres (9:00 hora local), colocando ordens buy-stop e sell-stop a distâncias configuráveis do preço atual. O objetivo é capturar o movimento de impulso pós-abertura, mantendo uma gestão de risco disciplinada por meio de níveis de stop-loss e take-profit medidos em pips.

## Lógica de negociação

1. A estratégia monitora velas concluídas de um período configurável (padrão de 1 minuto) para trabalhar com marcas de tempo da bolsa.
2. Cada novo dia de negociação reinicia o estado de configuração para que apenas um straddle seja preparado por sessão.
3. Assim que o tempo da vela atinge o "Look Hour" e o "Look Minute" configurados, a estratégia:
   - Cancela quaisquer ordens ativas restantes e fecha posições abertas para evitar conflitos.
   - Calcula preços de entrada, stop-loss e take-profit ajustados em pips usando o passo de preço do instrumento.
   - Coloca tanto uma ordem buy-stop quanto uma sell-stop nas distâncias em pips especificadas do último preço de fechamento.
4. Quando um lado é executado, a ordem pendente oposta é cancelada imediatamente. A estratégia então acompanha a ação do preço para sair da posição assim que o nível de stop-loss ou take-profit é atingido intradiário.
5. Um "Close Hour" diário opcional força a estratégia a nivelar posições e remover ordens pendentes ao final da sessão de Londres.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `Volume` | Tamanho de ordem usado para ambos os lados do straddle.
| `LookHour` | Hora da bolsa (0-23) que representa as 9 AM de Londres em seu feed de dados.
| `LookMinute` | Offset de minutos dentro do look hour quando as ordens devem ser preparadas.
| `CloseHour` | Hora em que todas as posições e ordens são fechadas à força.
| `UseCloseHour` | Habilita ou desabilita o comportamento de fechamento automático por hora.
| `TakeProfitPips` | Distância em pips do preço de entrada até o alvo de lucro para ambas as direções.
| `BuyDistancePips` | Distância em pips acima do preço atual para a ordem buy-stop.
| `SellDistancePips` | Distância em pips abaixo do preço atual para a ordem sell-stop.
| `BuyStopLossPips` | Distância de stop-loss em pips para posições compradas.
| `SellStopLossPips` | Distância de stop-loss em pips para posições vendidas.
| `CandleType` | Assinatura de velas usada para timing e gestão de saída (padrão: período de 1 minuto).

Todas as distâncias em pips se adaptam automaticamente a cotações FX de 3 ou 5 dígitos multiplicando o passo de preço da bolsa por dez quando necessário, espelhando o assessor especializado original.

## Gestão de risco

- A estratégia sempre emite alvos simétricos de stop-loss e take-profit em torno do preço de gatilho para manter um perfil de risco equilibrado.
- A liquidação ao final do dia garante que a conta não carregue exposição noturna, a menos que o parâmetro `UseCloseHour` seja desabilitado.
- Como as ordens são reemitidas apenas uma vez por dia, a estratégia evita o excesso de negociação durante sessões de consolidação.

## Notas de uso

1. Defina `LookHour` para corresponder às 9 AM hora de Londres no fuso horário do seu broker. Por exemplo, se o feed for UTC+1, use `LookHour = 10`.
2. Calibre as distâncias em pips para acomodar a volatilidade atual do GBP/USD ou seu par GBP preferido.
3. Implante a estratégia em símbolos FX que exponham bid/ask confiável e metadados de passo de preço para que os cálculos de pips permaneçam precisos.
4. Monitore as margens do broker: valores maiores de `Volume` podem exigir ajustes na alavancagem da conta, assim como a versão MQL original fazia.

## Arquivos

- `CS/Gbp9AmBreakoutStrategy.cs` – Implementação em C# usando a API de alto nível do StockSharp.
- `README.md` – Documentação em inglês (este arquivo).
- `README_ru.md` – Documentação em russo.
- `README_zh.md` – Documentação em chinês.

A implementação em Python é intencionalmente omitida conforme os requisitos do projeto.
