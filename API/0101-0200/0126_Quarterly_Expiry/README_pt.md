# Estratégia de Vencimento Trimestral
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
As semanas de Vencimento Trimestral veem contratos de futuros e opções sendo rolados, criando frequentemente volatilidade enquanto posições são fechadas ou roladas.
As oscilações de preços podem se acelerar quando as proteções são ajustadas e a liquidez temporariamente diminui.

Os testes indicam um retorno anual médio de aproximadamente 115%. Funciona melhor no mercado de ações.

A estratégia opera na direção da tendência predominante no início da semana, saindo antes do dia de liquidação para evitar o caos.

Um stop fixo mantém o risco sob controle se a volatilidade se mostrar excessiva.

## Detalhes

- **Critérios de entrada**: ativadores de efeito de calendário
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Sazonalidade
  - Direção: Ambos
  - Indicadores: Sazonalidade
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

