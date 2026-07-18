# Assistente de escalpelamento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Scalping Assistant** é uma conversão direta do MetaTrader 4 consultor especialista "Scalper Assistant v1.0". Ele não gera entradas por conta própria. Em vez disso, ele monitora as posições abertas na segurança configurada e gerencia as ordens de proteção de maneira semelhante a MetaTrader.

## Como funciona

1. Quando uma nova posição é detectada, a estratégia registra imediatamente ordens de stop-loss e take-profit usando as distâncias configuradas (expressas em etapas de preço).
2. A estratégia assina dados de nível 1 e rastreia continuamente a melhor oferta/venda para estimar o lucro atual da posição.
3. Assim que o lucro não realizado atingir `BreakEvenTriggerPoints`, o stop inicial é cancelado e registrado novamente ao preço de equilíbrio mais o deslocamento configurado.
4. O nível de stop permanece no ponto de equilíbrio; nenhum rastreamento adicional é executado. A ordem de lucro permanece intacta.
5. Assim que a posição for fechada, todas as ordens de proteção serão canceladas e o estado interno será redefinido, ficando pronto para a próxima negociação manual.

## Notas de uso

- Anexe a estratégia a um conector/carteira e abra negociações manualmente ou a partir de outro algoritmo. O auxiliar assumirá a proteção desses cargos.
- A lógica depende de cotações de nível 1; certifique-se de que o conector selecionado forneça as melhores atualizações de compra/venda.
- O termo *pontos* refere-se à etapa de preço do instrumento (`Security.PriceStep`). Para símbolos forex com cinco casas decimais, isso equivale a um pip.

## Parâmetros

| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `StopLossPoints` | `decimal` | `30` | Distância (em etapas de preço) usada ao colocar o stop de proteção inicial. Defina como `0` para ignorar o envio de uma ordem de parada. |
| `TakeProfitPoints` | `decimal` | `100` | Distância (em etapas de preço) usada ao colocar a ordem inicial de lucro. Defina como `0` para pular o take-profit. |
| `BreakEvenTriggerPoints` | `decimal` | `15` | Lucro em etapas de preço que devem ser alcançadas antes que o stop seja movido para o ponto de equilíbrio. |
| `BreakEvenOffsetPoints` | `decimal` | `5` | Distância extra (em etapas de preço) adicionada acima/abaixo do preço de entrada quando o stop é deslocado para o ponto de equilíbrio. |

## Status de conversão

- ✅ Lógica central: tratamento do ponto de equilíbrio com base em MetaTrader parâmetros de entrada.
- ✅ Uso de API de alto nível: `SubscribeLevel1()` com vinculação de delegado.
- ✅ Ordens de proteção: criadas por ajudantes `SellStop`, `BuyStop`, `SellLimit` e `BuyLimit`.
- ❌ Sem porta Python – apenas a estratégia C# é fornecida, correspondendo à solicitação.
