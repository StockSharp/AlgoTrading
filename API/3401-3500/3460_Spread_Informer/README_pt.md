# Estratégia de divulgação do informador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Coleta estatísticas detalhadas para o spread de compra e venda do instrumento selecionado e notifica quando o spread ultrapassa um limite configurável. A estratégia escuta continuamente as atualizações do Nível 1, rastreia o spread máximo, mínimo e médio em pontos e registra um resumo quando para. É útil para pesquisar as condições de liquidez antes de executar sistemas sensíveis à latência ou otimizar janelas de negociação no Testador de Estratégia.

## Detalhes

- **Fonte de dados**: melhor lance de nível 1 e melhores cotações de venda.
- **Estatísticas capturadas**:
  - Carimbos de data e hora de início e término do período de observação.
  - Spread máximo e o momento em que ocorreu.
  - Spread mínimo e o momento em que ocorreu.
  - Spread médio calculado em todas as amostras de Nível 1 observadas.
- **Alertas**:
  - Alerta opcional quando o spread (em pontos) ultrapassa o limite `MaxSpreadPoints` configurado.
  - A frequência de alerta é limitada por `AlertIntervalSeconds` para evitar spam no log.
  - Os alertas só são acionados quando o spread ultrapassa o limite por baixo.
- **Registro**:
  - Alertas em tempo real são gravados por meio de `LogInfo`.
  - O resumo final das estatísticas é emitido durante `OnStopped`.
- **Valores padrão**:
  - `MaxSpreadPoints` = 0 (alertas desabilitados).
  - `AlertIntervalSeconds` = 0 (sem limitação).

## Parâmetros

| Nome | Descrição | Padrão | Notas |
| --- | --- | --- | --- |
| `MaxSpreadPoints` | Spread máximo permitido em pontos. Defina como 0 para desativar alertas. | 0 | Os pontos são calculados usando a etapa de preço do instrumento. |
| `AlertIntervalSeconds` | Tempo mínimo entre alertas consecutivos. | 0 | Evita alertas duplicados quando a propagação permanece ampla. |

## Notas de uso

1. Anexe a estratégia a um instrumento e garanta que os dados do Nível 1 estejam disponíveis.
2. Configure `MaxSpreadPoints` de acordo com o spread aceitável para o instrumento.
3. Opcionalmente, aumente `AlertIntervalSeconds` para suprimir notificações repetidas durante períodos voláteis.
4. Pare a estratégia para revisar as estatísticas registradas na saída do terminal.
