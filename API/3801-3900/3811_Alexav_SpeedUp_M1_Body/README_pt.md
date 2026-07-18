# Estratégia Alexav SpeedUp M1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Alexav SpeedUp M1** é uma versão direta do MetaTrader 4 consultor especialista "Alexav_SpeedUp_M1". Ele avalia os corpos das velas intradiárias concluídas e reage imediatamente às ordens de mercado sempre que o corpo da vela excede um limite configurável. Após uma entrada, a estratégia emula o gerenciamento de risco no estilo MetaTrader, anexando ordens de stop-loss, take-profit e trailing stop à posição aberta.

A conversão depende do StockSharp API de alto nível. As velas são consumidas por meio de `SubscribeCandles`, as informações de preço para rastreamento são recebidas dos dados de nível 1 e os pedidos de proteção são feitos com ajudantes padrão `BuyStop`, `SellStop`, `BuyLimit` e `SellLimit`. Não são necessários cálculos manuais de indicadores.

## Geração de sinal
1. Cada vela finalizada no período configurado é inspecionada.
2. Quando a vela fecha acima de sua abertura em mais de **Body Threshold**, a estratégia abre (ou reverte para) uma posição longa no mercado.
3. Quando a vela fecha abaixo de sua abertura em mais do que o mesmo limite, a estratégia abre (ou reverte para) uma posição curta no mercado.
4. A exposição existente na direção oposta é fechada automaticamente aumentando o volume da ordem de mercado, reproduzindo fielmente o comportamento do consultor especialista original.

## Gerenciamento de pedidos
* **Stop-loss inicial**: Assim que o volume da posição aumenta, uma ordem de stop de proteção é registrada pelo preço de entrada menos (para posições compradas) ou mais (para posições vendidas) o número de pontos configurado.
* **Take Profit**: Uma ordem com limite correspondente é colocada na direção da negociação na distância especificada por **Take Profit (pontos)**.
* **Trailing stop**: as atualizações de oferta/venda de nível 1 monitoram o lucro atual. Quando o lucro não realizado excede a distância de fuga, o stop de proteção é movido em direção ao preço, mantendo o gap configurado sem nunca recuar.
* Todas as ordens de proteção são canceladas sempre que a posição retorna à estabilidade.

A conversão mantém a lógica intencionalmente simples: nenhum filtro, indicador ou controle de risco adicional é adicionado além do que estava presente na implementação do MQL.

## Parâmetros
| Nome | Descrição |
| ---- | ----------- |
| **Tamanho do lote** | Volume base de negociação (em lotes) utilizado para cada ordem de mercado. Ao inverter uma posição existente, o volume necessário é adicionado automaticamente. |
| **Take Profit (pontos)** | Distância do preço de entrada ao nível de lucro medido em MetaTrader pontos (convertido usando a etapa de preço do título). |
| **Parada inicial (pontos)** | Distância do preço de entrada ao stop de proteção inicial expressa em pontos. |
| **Trailing Stop (pontos)** | A distância final é mantida após o preço se mover a favor da posição. Um valor zero desativa a lógica final. |
| **Limiar Corporal** | Diferença absoluta mínima entre o fechamento e a abertura da vela necessária para acionar uma nova negociação. |
| **Tipo de vela** | Série de velas (período de tempo) usada para avaliação de sinal. O padrão corresponde ao gráfico original de um minuto. |

## Notas de uso
* Certifique-se de que a segurança forneça um `PriceStep` válido. Quando indisponível, a estratégia volta a interpretar as distâncias dos pontos como compensações de preços brutos.
* A lógica de trailing stop requer dados de nível 1 (melhor oferta/venda). Quando apenas os dados da vela estão disponíveis, a funcionalidade de rastreamento permanece inativa.
* A estratégia foi projetada para operação intradiária e reflete o comportamento de uma negociação por vela aplicado pelo especialista MQL original por meio de seus contadores internos.
