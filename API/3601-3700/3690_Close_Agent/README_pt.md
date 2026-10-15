# Fechar estratégia de agente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Close Agent Strategy é um assistente de gerenciamento de risco que reflete o consultor especialista MQL CloseAgent. A estratégia não abre novas negociações. Em vez disso, ele monitora as posições existentes e as fecha quando o preço ultrapassa as bandas Bollinger enquanto o Índice de Força Relativa (RSI) atinge zonas extremas. A ferramenta pode observar posições criadas manualmente ou por outras estratégias automatizadas e, opcionalmente, liquidar tudo assim que uma meta de lucro global for alcançada.

## Indicadores e Dados
- **Velas:** intervalo de tempo configurável (padrão: 5 minutos) usado para calcular os indicadores.
- **Bollinger Bandas (comprimento 21, largura 2):** detecta variações de preço acima da banda superior ou abaixo da banda inferior.
- **Índice de Força Relativa (comprimento 13):** confirma se o mercado está sobrecomprado (>70) ou sobrevendido (<30).
- **Dados de nível 1:** capturam as cotações de lance e solicitação mais recentes para avaliar as condições de saída com a maior precisão possível.

## Parâmetros
- **Modo Fechar (`CloseMode`):** seleciona quais posições são elegíveis para fechamento.
  - `Manual` – apenas posições sem este identificador de estratégia (negociações manuais ou outros bots).
  - `Auto` – apenas posições abertas por esta instância de estratégia.
  - `Both` – monitore todas as posições no símbolo de estratégia.
- **Tipo de vela (`CandleType`):** intervalo de tempo usado para calcular Bollinger Bandas e RSI.
- **Modo de operação (`OperationMode`):**
  - `LiveBar` – use a última vela em formação; reage mais rápido, mas pode usar dados inacabados.
  - `NewBar` – espera o fechamento de uma vela antes de gerar um sinal (mais seguro, porém mais lento).
- **Close All Target (`CloseAllTarget`):** se o lucro flutuante (`PnL`) atingir esse valor absoluto, cada posição monitorada é fechada imediatamente.
- **Ativar alertas (`EnableAlerts`):** quando verdadeiro, registra uma mensagem sempre que uma saída é acionada, incluindo a estimativa de lucro realizado.

## Lógica de negociação
1. Assina as cotações do Nível 1 e a série de velas configurada. Bollinger Bandas e RSI são atualizadas para cada vela recebida.
2. Mantém um buffer de histórico compacto para que a estratégia possa fazer referência à vela fechada mais recente quando `OperationMode` estiver definido como `NewBar`.
3. Verifica se a meta de lucro global foi atingida. Quando `CloseAllTarget` > 0 e `PnL` excede o limite, todas as posições elegíveis são niveladas a preços de mercado.
4. Para cada posição monitorada no símbolo de estratégia:
   - **Posições longas:** fechadas quando o melhor lance está acima da faixa superior Bollinger, RSI está acima de 70 e o preço permanece acima do preço médio de entrada.
   - **Posições curtas:** fechadas quando a melhor venda está abaixo da faixa inferior Bollinger, RSI está abaixo de 30 e o preço permanece abaixo do preço médio de entrada.
5. Usa cotações de compra/venda quando disponíveis; caso contrário, volta para o fechamento da última vela processada para evitar saídas perdidas.

## Notas de uso
- A estratégia é concebida como uma camada protetora e pressupõe que as posições podem ser abertas externamente.
- Como a lógica atua apenas nas saídas do mercado, a estratégia deve funcionar em conjunto com outros sistemas de negociação para gerir a exposição ao risco.
- Os alertas aparecem no log do Designer quando `EnableAlerts` está ativo, correspondendo aos alertas originais do MQL.
