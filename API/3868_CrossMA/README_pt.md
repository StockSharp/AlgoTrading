# Estratégia de notificação cruzada MA ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp do consultor especialista MetaTrader 4 "CrossMA". Ele negocia o cruzamento entre duas médias móveis simples e protege cada negociação com um stop loss baseado em Average True Range (ATR). Além da lógica original, a estratégia registra mensagens de informações detalhadas em vez de enviar e-mails.

## Lógica de negociação
1. A estratégia se inscreve na série de velas configurada e calcula uma média móvel simples rápida e lenta junto com um indicador ATR.
2. Quando o SMA rápido cruza acima do SMA lento, qualquer exposição curta é fechada e uma posição longa é aberta. O stop loss é colocado um ATR abaixo do preço de entrada.
3. Quando o SMA rápido cruza abaixo do SMA lento, qualquer exposição longa é fechada e uma posição curta é aberta. O stop loss é colocado um ATR acima do preço de entrada.
4. Em cada vela finalizada, o preço stop é verificado. Se o preço atingir o nível de stop, a posição será imediatamente fechada no mercado.

## Gestão de risco
- O tamanho da posição é calculado a partir do patrimônio da conta e do parâmetro `Maximum Risk`. Se as informações de patrimônio não estiverem disponíveis, a estratégia volta ao valor `Base Volume`.
- Após duas ou mais negociações perdedoras consecutivas, o tamanho da posição é reduzido proporcionalmente ao `Decrease Factor`, reproduzindo o comportamento original do MetaTrader.
- Todos os volumes são normalizados para a etapa de volume de segurança para garantir tamanhos de pedido válidos.

## Notificações
Em vez de enviar e-mails, a estratégia escreve mensagens de log claras sempre que as ordens são abertas ou fechadas por sinais ou paradas. O registro pode ser desativado por meio do parâmetro `Enable Notifications`.

## Parâmetros
- **Tipo de vela** – tipo de vela usado para cálculos de indicadores.
- **Período SMA rápido** – período da média móvel rápida (padrão 4).
- **Período lento SMA** – período da média móvel lenta (padrão 12).
- **ATR Período** – número de velas usadas por ATR para o cálculo do stop (padrão 6).
- **Volume Base** – volume mínimo negociado quando o dimensionamento baseado em risco não está disponível (padrão 0,1).
- **Risco Máximo** – fração do patrimônio alocado em cada negociação (padrão 0,02).
- **Fator de Diminuição** – reduz o tamanho da posição após perder negociações (padrão 3).
- **Ativar notificações** – permite o registro de ações comerciais.
